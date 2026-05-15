using System.Globalization;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Dunning;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class DunningRepository(EasyMittDbContext db) : IDunningRepository
{
    public async Task<DunningOverviewDto> GetOverviewAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken)
    {
        var invoices = await db.InvoiceDrafts.AsNoTracking()
            .Include(x => x.Customer)
            .Where(x =>
                x.CompanyId == companyId &&
                x.Status != InvoiceLifecycleStatus.Draft &&
                x.Status != InvoiceLifecycleStatus.Cancelled &&
                x.Status != InvoiceLifecycleStatus.Paid)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(300)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paidByInvoice = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new { InvoiceDraftId = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.InvoiceDraftId, x => x.Amount, cancellationToken);

        var reminders = await db.DunningReminders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new ReminderState(x.Key, x.Max(y => y.Level), x.Max(y => y.CreatedAtUtc)))
            .ToDictionaryAsync(x => x.InvoiceDraftId, x => x, cancellationToken);

        var rows = invoices
            .Select(invoice => ToDunningInvoice(invoice, today, paidByInvoice.GetValueOrDefault(invoice.Id), reminders.GetValueOrDefault(invoice.Id)))
            .Where(x => x.OpenAmount > 0 && x.DaysOverdue > 0)
            .OrderByDescending(x => x.DaysOverdue)
            .ThenByDescending(x => x.OpenAmount)
            .ToArray();

        var customers = rows
            .GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(group => new DunningCustomerSummaryDto
            {
                CustomerId = group.Key.CustomerId,
                CustomerName = group.Key.CustomerName,
                OverdueInvoiceCount = group.Count(),
                OpenAmount = group.Sum(x => x.OpenAmount),
                HighestReminderLevel = group.Max(x => x.ReminderLevel),
                LastReminderAtUtc = group.Max(x => x.LastReminderAtUtc),
            })
            .OrderByDescending(x => x.OpenAmount)
            .ToArray();

        var collectedAmount = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(x => x.Amount, cancellationToken);

        return new DunningOverviewDto
        {
            Invoices = rows,
            Customers = customers,
            TotalOpenAmount = rows.Sum(x => x.OpenAmount),
            OverdueInvoiceCount = rows.Length,
            ReminderDueCount = rows.Count(IsReminderDue),
            CollectedAmount = collectedAmount,
        };
    }

    public async Task<IReadOnlyList<DunningReminderDto>> GetInvoiceRemindersAsync(Guid companyId, Guid invoiceDraftId, CancellationToken cancellationToken) =>
        await db.DunningReminders.AsNoTracking()
            .Include(x => x.InvoiceDraft)
            .Where(x => x.CompanyId == companyId && x.InvoiceDraftId == invoiceDraftId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => ToReminderDto(x, x.InvoiceDraft == null ? "" : InvoiceNumber(x.InvoiceDraft.PayloadJson)))
            .ToArrayAsync(cancellationToken);

    public async Task<DunningReminderDto?> CreateReminderAsync(Guid companyId, Guid userId, string userEmail, DunningReminderCreateDto request, CancellationToken cancellationToken)
    {
        var invoice = await db.InvoiceDrafts
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == request.InvoiceDraftId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var paid = await db.PaymentAllocations
            .Where(x => x.CompanyId == companyId && x.InvoiceDraftId == invoice.Id)
            .SumAsync(x => x.Amount, cancellationToken);
        var total = InvoiceTotal(invoice.PayloadJson);
        var openAmount = Math.Max(0, total - paid);
        if (openAmount <= 0)
        {
            throw new InvalidOperationException("dunning_invoice_paid");
        }

        var currentLevel = await db.DunningReminders
            .Where(x => x.CompanyId == companyId && x.InvoiceDraftId == invoice.Id)
            .Select(x => (int?)x.Level)
            .MaxAsync(cancellationToken) ?? 0;

        var entity = new DunningReminderEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            InvoiceDraftId = invoice.Id,
            Level = Math.Min(currentLevel + 1, 4),
            OpenAmount = openAmount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            UserId = userId,
            UserEmail = userEmail.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        if (InvoiceLifecyclePolicy.CanTransition(invoice.Status, InvoiceLifecycleStatus.Overdue))
        {
            invoice.Status = InvoiceLifecycleStatus.Overdue;
            invoice.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.DunningReminders.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToReminderDto(entity, InvoiceNumber(invoice.PayloadJson));
    }

    private static bool IsReminderDue(DunningInvoiceDto invoice) =>
        invoice.LastReminderAtUtc is null || invoice.LastReminderAtUtc.Value <= DateTime.UtcNow.AddDays(-7);

    private static DunningInvoiceDto ToDunningInvoice(InvoiceDraftEntity invoice, DateOnly today, decimal paidAmount, ReminderState? reminder)
    {
        var issueDate = InvoiceIssueDate(invoice.PayloadJson, DateOnly.FromDateTime(invoice.CreatedAtUtc));
        var dueDate = issueDate.AddDays(invoice.Customer?.PaymentTermsDays ?? 14);
        var total = InvoiceTotal(invoice.PayloadJson);
        return new DunningInvoiceDto
        {
            InvoiceDraftId = invoice.Id,
            CustomerId = invoice.CustomerId,
            InvoiceNumber = InvoiceNumber(invoice.PayloadJson),
            CustomerName = invoice.Customer?.DisplayName ?? BuyerName(invoice.PayloadJson),
            CustomerEmail = invoice.Customer?.Email,
            IssueDate = issueDate,
            DueDate = dueDate,
            DaysOverdue = Math.Max(0, today.DayNumber - dueDate.DayNumber),
            InvoiceTotal = total,
            PaidAmount = paidAmount,
            OpenAmount = Math.Max(0, total - paidAmount),
            InvoiceStatus = invoice.Status,
            ReminderLevel = reminder?.Level ?? 0,
            LastReminderAtUtc = reminder?.LastReminderAtUtc,
        };
    }

    private static DunningReminderDto ToReminderDto(DunningReminderEntity x, string invoiceNumber) => new()
    {
        Id = x.Id,
        InvoiceDraftId = x.InvoiceDraftId,
        InvoiceNumber = invoiceNumber,
        Level = x.Level,
        LevelName = LevelName(x.Level),
        OpenAmount = x.OpenAmount,
        Note = x.Note,
        CreatedAtUtc = x.CreatedAtUtc,
        UserId = x.UserId,
        UserEmail = x.UserEmail,
    };

    private static string LevelName(int level) => level switch
    {
        1 => "FriendlyReminder",
        2 => "FirstMahnung",
        3 => "SecondMahnung",
        _ => "FinalNotice",
    };

    private static decimal InvoiceTotal(string payloadJson) => ReadDecimal(payloadJson, "core", "BT-112");

    private static string InvoiceNumber(string payloadJson) => ReadString(payloadJson, "core", "BT-1");

    private static string BuyerName(string payloadJson) => ReadString(payloadJson, "buyer", "BT-26");

    private static DateOnly InvoiceIssueDate(string payloadJson, DateOnly fallback)
    {
        var text = ReadString(payloadJson, "core", "BT-2");
        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : fallback;
    }

    private static string ReadString(string payloadJson, string section, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty(section, out var sectionElement) &&
                sectionElement.TryGetProperty(property, out var value)
                    ? value.ToString()
                    : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static decimal ReadDecimal(string payloadJson, string section, string property)
    {
        var text = ReadString(payloadJson, section, property);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private sealed record ReminderState(Guid InvoiceDraftId, int Level, DateTime LastReminderAtUtc);
}
