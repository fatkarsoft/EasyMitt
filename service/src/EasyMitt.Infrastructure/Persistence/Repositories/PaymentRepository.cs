using System.Globalization;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Payments;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(EasyMittDbContext db) : IPaymentRepository
{
    public async Task<IReadOnlyList<BankTransactionDto>> SearchTransactionsAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim().ToLowerInvariant();
        var normalizedStatus = status?.Trim();
        var transactions = db.BankTransactions.AsNoTracking()
            .Include(x => x.Allocations)
            .Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            transactions = transactions.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            transactions = transactions.Where(x =>
                x.Description.ToLower().Contains(normalizedQuery) ||
                (x.CounterpartyName != null && x.CounterpartyName.ToLower().Contains(normalizedQuery)) ||
                (x.CounterpartyIban != null && x.CounterpartyIban.ToLower().Contains(normalizedQuery)));
        }

        var entities = await transactions
            .OrderByDescending(x => x.BookingDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToArray();
    }

    public async Task<BankTransactionDto?> GetTransactionAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.BankTransactions.AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<BankTransactionDto> CreateTransactionAsync(Guid companyId, BankTransactionCreateDto request, CancellationToken cancellationToken)
    {
        var entity = NewTransaction(companyId, request);
        db.BankTransactions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<BankTransactionDto>> ImportTransactionsAsync(Guid companyId, IReadOnlyList<BankTransactionCreateDto> requests, CancellationToken cancellationToken)
    {
        var entities = requests.Select(request => NewTransaction(companyId, new BankTransactionCreateDto
        {
            BookingDate = request.BookingDate,
            Description = request.Description,
            CounterpartyName = request.CounterpartyName,
            CounterpartyIban = request.CounterpartyIban,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            Source = request.Source ?? "CsvImport",
        })).ToArray();
        db.BankTransactions.AddRange(entities);
        await db.SaveChangesAsync(cancellationToken);
        return entities.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<PaymentSuggestionDto>> SuggestInvoicesAsync(Guid companyId, Guid bankTransactionId, CancellationToken cancellationToken)
    {
        var transaction = await db.BankTransactions.AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == bankTransactionId, cancellationToken);
        if (transaction is null)
        {
            return Array.Empty<PaymentSuggestionDto>();
        }

        var invoices = await db.InvoiceDrafts.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status != InvoiceLifecycleStatus.Draft && x.Status != InvoiceLifecycleStatus.Cancelled && x.Status != InvoiceLifecycleStatus.Paid)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paidByInvoice = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new { InvoiceDraftId = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.InvoiceDraftId, x => x.Amount, cancellationToken);

        return invoices
            .Select(invoice => BuildSuggestion(transaction, invoice, paidByInvoice.GetValueOrDefault(invoice.Id)))
            .Where(x => x.OpenAmount > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => Math.Abs(x.OpenAmount - Math.Abs(transaction.Amount)))
            .Take(10)
            .ToArray();
    }

    public async Task<PaymentAllocationDto?> AllocateAsync(Guid companyId, PaymentAllocationCreateDto request, CancellationToken cancellationToken)
    {
        var transaction = await db.BankTransactions
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == request.BankTransactionId, cancellationToken);
        var invoice = await db.InvoiceDrafts.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == request.InvoiceDraftId, cancellationToken);
        if (transaction is null || invoice is null)
        {
            return null;
        }

        var amount = decimal.Round(request.Amount, 2);
        if (amount <= 0)
        {
            throw new InvalidOperationException("payment_amount_invalid");
        }

        var transactionOpen = Math.Abs(transaction.Amount) - transaction.Allocations.Sum(x => x.Amount);
        if (amount > transactionOpen)
        {
            throw new InvalidOperationException("payment_amount_exceeds_transaction");
        }

        var invoiceTotal = InvoiceTotal(invoice.PayloadJson);
        var invoicePaid = await db.PaymentAllocations
            .Where(x => x.CompanyId == companyId && x.InvoiceDraftId == invoice.Id)
            .SumAsync(x => x.Amount, cancellationToken);
        if (amount > invoiceTotal - invoicePaid)
        {
            throw new InvalidOperationException("payment_amount_exceeds_invoice");
        }

        var allocation = new PaymentAllocationEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BankTransactionId = transaction.Id,
            InvoiceDraftId = invoice.Id,
            Amount = amount,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.PaymentAllocations.Add(allocation);
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        transaction.Status = PaymentStatus(Math.Abs(transaction.Amount), transaction.Allocations.Sum(x => x.Amount) + amount);
        UpdateInvoicePaymentStatus(invoice, invoiceTotal, invoicePaid + amount);

        await db.SaveChangesAsync(cancellationToken);
        return ToAllocationDto(allocation, invoice);
    }

    public async Task<InvoicePaymentSummaryDto?> GetInvoiceSummaryAsync(Guid companyId, Guid invoiceDraftId, CancellationToken cancellationToken)
    {
        var invoice = await db.InvoiceDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == invoiceDraftId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        var allocations = await db.PaymentAllocations.AsNoTracking()
            .Include(x => x.BankTransaction)
            .Where(x => x.CompanyId == companyId && x.InvoiceDraftId == invoiceDraftId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var total = InvoiceTotal(invoice.PayloadJson);
        var paid = allocations.Sum(x => x.Amount);
        return new InvoicePaymentSummaryDto
        {
            InvoiceDraftId = invoice.Id,
            InvoiceNumber = InvoiceNumber(invoice.PayloadJson),
            InvoiceTotal = total,
            PaidAmount = paid,
            OpenAmount = Math.Max(0, total - paid),
            PaymentStatus = paid <= 0 ? "Unpaid" : paid >= total ? "Paid" : "PartiallyPaid",
            Allocations = allocations.Select(x => ToAllocationDto(x, invoice)).ToArray(),
        };
    }

    private static BankTransactionEntity NewTransaction(Guid companyId, BankTransactionCreateDto request)
    {
        var amount = decimal.Round(request.Amount, 2);
        var now = DateTime.UtcNow;
        return new BankTransactionEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BookingDate = request.BookingDate,
            Description = request.Description.Trim(),
            CounterpartyName = string.IsNullOrWhiteSpace(request.CounterpartyName) ? null : request.CounterpartyName.Trim(),
            CounterpartyIban = string.IsNullOrWhiteSpace(request.CounterpartyIban) ? null : request.CounterpartyIban.Replace(" ", "").Trim().ToUpperInvariant(),
            Amount = amount,
            Direction = amount >= 0 ? "Incoming" : "Outgoing",
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "EUR" : request.CurrencyCode.Trim().ToUpperInvariant(),
            Status = "Unmatched",
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Manual" : request.Source.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    private static PaymentSuggestionDto BuildSuggestion(BankTransactionEntity transaction, InvoiceDraftEntity invoice, decimal paidAmount)
    {
        var invoiceNumber = InvoiceNumber(invoice.PayloadJson);
        var buyerName = BuyerName(invoice.PayloadJson);
        var total = InvoiceTotal(invoice.PayloadJson);
        var openAmount = Math.Max(0, total - paidAmount);
        var description = transaction.Description.ToLowerInvariant();
        var counterparty = (transaction.CounterpartyName ?? "").ToLowerInvariant();
        var reasons = new List<string>();
        var score = 0;

        if (!string.IsNullOrWhiteSpace(invoiceNumber) && description.Contains(invoiceNumber.ToLowerInvariant()))
        {
            score += 55;
            reasons.Add("invoice_number");
        }

        if (!string.IsNullOrWhiteSpace(buyerName) && (description.Contains(buyerName.ToLowerInvariant()) || counterparty.Contains(buyerName.ToLowerInvariant())))
        {
            score += 25;
            reasons.Add("buyer_name");
        }

        if (Math.Abs(Math.Abs(transaction.Amount) - openAmount) <= 0.01m)
        {
            score += 35;
            reasons.Add("amount_exact");
        }
        else if (Math.Abs(transaction.Amount) < openAmount)
        {
            score += 10;
            reasons.Add("partial_amount");
        }

        return new PaymentSuggestionDto
        {
            InvoiceDraftId = invoice.Id,
            InvoiceNumber = invoiceNumber,
            BuyerName = buyerName,
            IssueDate = InvoiceIssueDate(invoice.PayloadJson, DateOnly.FromDateTime(invoice.CreatedAtUtc)),
            InvoiceTotal = total,
            PaidAmount = paidAmount,
            OpenAmount = openAmount,
            Score = Math.Min(score, 100),
            Reasons = reasons,
        };
    }

    private static void UpdateInvoicePaymentStatus(InvoiceDraftEntity invoice, decimal total, decimal paid)
    {
        var next = paid >= total ? InvoiceLifecycleStatus.Paid : InvoiceLifecycleStatus.PartiallyPaid;
        if (InvoiceLifecyclePolicy.CanTransition(invoice.Status, next))
        {
            invoice.Status = next;
            invoice.UpdatedAtUtc = DateTime.UtcNow;
            if (next == InvoiceLifecycleStatus.Paid)
            {
                invoice.PaidAtUtc ??= DateTime.UtcNow;
            }
        }
    }

    private static string PaymentStatus(decimal total, decimal paid) =>
        paid <= 0 ? "Unmatched" : paid >= total ? "Matched" : "PartiallyMatched";

    private static BankTransactionDto ToDto(BankTransactionEntity x)
    {
        var matched = x.Allocations.Sum(a => a.Amount);
        return new BankTransactionDto
        {
            Id = x.Id,
            BookingDate = x.BookingDate,
            Description = x.Description,
            CounterpartyName = x.CounterpartyName,
            CounterpartyIban = x.CounterpartyIban,
            Amount = x.Amount,
            MatchedAmount = matched,
            UnmatchedAmount = Math.Max(0, Math.Abs(x.Amount) - matched),
            Direction = x.Direction,
            CurrencyCode = x.CurrencyCode,
            Status = x.Status,
            Source = x.Source,
            CreatedAtUtc = x.CreatedAtUtc,
            Allocations = x.Allocations.Select(a => new PaymentAllocationDto
            {
                Id = a.Id,
                BankTransactionId = a.BankTransactionId,
                InvoiceDraftId = a.InvoiceDraftId,
                Amount = a.Amount,
                CreatedAtUtc = a.CreatedAtUtc,
            }).ToArray(),
        };
    }

    private static PaymentAllocationDto ToAllocationDto(PaymentAllocationEntity allocation, InvoiceDraftEntity invoice) => new()
    {
        Id = allocation.Id,
        BankTransactionId = allocation.BankTransactionId,
        InvoiceDraftId = allocation.InvoiceDraftId,
        InvoiceNumber = InvoiceNumber(invoice.PayloadJson),
        Amount = allocation.Amount,
        CreatedAtUtc = allocation.CreatedAtUtc,
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
}
