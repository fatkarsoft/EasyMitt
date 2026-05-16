using System.Globalization;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Reporting;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class ReportingRepository(EasyMittDbContext db) : IReportingRepository
{
    public async Task<ReportingOverviewDto> GetOverviewAsync(Guid companyId, DateOnly from, DateOnly to, DateOnly today, CancellationToken cancellationToken)
    {
        var invoiceCandidates = await db.InvoiceDrafts.AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.CompanyId == companyId && x.Status != InvoiceLifecycleStatus.Cancelled && x.Status != InvoiceLifecycleStatus.Draft)
            .ToListAsync(cancellationToken);

        var invoices = invoiceCandidates
            .Select(entity => new InvoiceProjection(entity, ParsePayload(entity.PayloadJson)))
            .ToArray();

        var inRange = invoices
            .Where(x => x.IssueDate >= from && x.IssueDate <= to)
            .ToArray();

        var invoiceIds = invoices.Select(x => x.Entity.Id).ToArray();

        var paidByInvoice = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(g => new { InvoiceId = g.Key, Amount = g.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.InvoiceId, x => x.Amount, cancellationToken);

        var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusiveUtc = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var collectedAmount = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.CreatedAtUtc >= fromUtc
                && x.CreatedAtUtc < toExclusiveUtc)
            .SumAsync(x => x.Amount, cancellationToken);

        var expenses = await db.Expenses.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IssueDate >= from && x.IssueDate <= to)
            .ToListAsync(cancellationToken);

        var datevLogs = await db.DatevExportLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var revenueByMonth = BuildRevenueByMonth(inRange);
        var vatSummary = BuildVatSummary(inRange);
        var aging = BuildAging(invoices, paidByInvoice, today);
        var topRevenue = BuildTopRevenueCustomers(inRange);
        var topOverdue = BuildTopOverdueCustomers(invoices, paidByInvoice, today);
        var datevCoverage = BuildDatevCoverage(inRange, expenses, datevLogs);
        var expenseByCategory = BuildExpenseCategories(expenses);

        var revenueNet = inRange.Sum(x => x.NetAmount);
        var revenueTax = inRange.Sum(x => x.TaxAmount);
        var revenueGross = inRange.Sum(x => x.GrossAmount);
        var expenseTotal = expenses.Sum(x => x.TotalAmount);
        var openReceivables = invoices.Sum(x => Math.Max(0, x.GrossAmount - paidByInvoice.GetValueOrDefault(x.Entity.Id)));
        var overdueCount = invoices.Count(x =>
        {
            var dueDate = x.IssueDate.AddDays(x.Entity.Customer?.PaymentTermsDays ?? 14);
            var paid = paidByInvoice.GetValueOrDefault(x.Entity.Id);
            return paid < x.GrossAmount && today > dueDate;
        });

        var summary = new ReportingSummaryDto
        {
            RevenueNet = revenueNet,
            RevenueTax = revenueTax,
            RevenueGross = revenueGross,
            OpenReceivables = openReceivables,
            CollectedAmount = collectedAmount,
            ExpenseTotal = expenseTotal,
            NetResult = revenueNet - expenses.Sum(x => x.NetAmount),
            IssuedInvoiceCount = inRange.Length,
            OverdueInvoiceCount = overdueCount,
            ExpenseCount = expenses.Count,
        };

        return new ReportingOverviewDto
        {
            From = from,
            To = to,
            Summary = summary,
            RevenueByMonth = revenueByMonth,
            VatSummary = vatSummary,
            Aging = aging,
            TopCustomersByRevenue = topRevenue,
            TopCustomersByOverdue = topOverdue,
            DatevCoverage = datevCoverage,
            ExpenseByCategory = expenseByCategory,
        };
    }

    private static IReadOnlyList<ReportingRevenuePointDto> BuildRevenueByMonth(IReadOnlyCollection<InvoiceProjection> invoices) =>
        invoices
            .GroupBy(x => new { x.IssueDate.Year, x.IssueDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ReportingRevenuePointDto
            {
                Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                Net = g.Sum(x => x.NetAmount),
                Tax = g.Sum(x => x.TaxAmount),
                Gross = g.Sum(x => x.GrossAmount),
                InvoiceCount = g.Count(),
            })
            .ToArray();

    private static IReadOnlyList<ReportingVatBucketDto> BuildVatSummary(IReadOnlyCollection<InvoiceProjection> invoices)
    {
        var buckets = new Dictionary<decimal, (decimal Net, decimal Tax)>();
        foreach (var invoice in invoices)
        {
            foreach (var line in invoice.Lines)
            {
                var key = Math.Round(line.RatePercent, 2);
                buckets.TryGetValue(key, out var current);
                var lineTax = line.NetAmount * (line.RatePercent / 100m);
                buckets[key] = (current.Net + line.NetAmount, current.Tax + lineTax);
            }
        }

        return buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new ReportingVatBucketDto
            {
                RatePercent = kv.Key,
                Net = Math.Round(kv.Value.Net, 2),
                Tax = Math.Round(kv.Value.Tax, 2),
                Gross = Math.Round(kv.Value.Net + kv.Value.Tax, 2),
            })
            .ToArray();
    }

    private static IReadOnlyList<ReportingAgingBucketDto> BuildAging(IReadOnlyCollection<InvoiceProjection> invoices, IReadOnlyDictionary<Guid, decimal> paidByInvoice, DateOnly today)
    {
        var ranges = new (string Bucket, int Min, int Max)[]
        {
            ("0-30", 1, 30),
            ("31-60", 31, 60),
            ("61-90", 61, 90),
            ("90+", 91, int.MaxValue),
        };

        var result = ranges
            .Select(range => new ReportingAgingBucketDto { Bucket = range.Bucket })
            .ToList();

        foreach (var invoice in invoices)
        {
            var paid = paidByInvoice.GetValueOrDefault(invoice.Entity.Id);
            var open = invoice.GrossAmount - paid;
            if (open <= 0) continue;

            var dueDate = invoice.IssueDate.AddDays(invoice.Entity.Customer?.PaymentTermsDays ?? 14);
            var daysOverdue = today.DayNumber - dueDate.DayNumber;
            if (daysOverdue <= 0) continue;

            for (var i = 0; i < ranges.Length; i++)
            {
                if (daysOverdue >= ranges[i].Min && daysOverdue <= ranges[i].Max)
                {
                    result[i] = new ReportingAgingBucketDto
                    {
                        Bucket = result[i].Bucket,
                        InvoiceCount = result[i].InvoiceCount + 1,
                        OpenAmount = result[i].OpenAmount + open,
                    };
                    break;
                }
            }
        }

        return result;
    }

    private static IReadOnlyList<ReportingCustomerPerformanceDto> BuildTopRevenueCustomers(IReadOnlyCollection<InvoiceProjection> invoices) =>
        invoices
            .GroupBy(x => new { x.Entity.CustomerId, Name = x.Entity.Customer?.DisplayName ?? x.BuyerName })
            .Select(g => new ReportingCustomerPerformanceDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = string.IsNullOrWhiteSpace(g.Key.Name) ? "-" : g.Key.Name,
                RevenueGross = g.Sum(x => x.GrossAmount),
                InvoiceCount = g.Count(),
            })
            .OrderByDescending(x => x.RevenueGross)
            .Take(5)
            .ToArray();

    private static IReadOnlyList<ReportingCustomerPerformanceDto> BuildTopOverdueCustomers(IReadOnlyCollection<InvoiceProjection> invoices, IReadOnlyDictionary<Guid, decimal> paidByInvoice, DateOnly today) =>
        invoices
            .Select(invoice =>
            {
                var paid = paidByInvoice.GetValueOrDefault(invoice.Entity.Id);
                var open = Math.Max(0, invoice.GrossAmount - paid);
                var dueDate = invoice.IssueDate.AddDays(invoice.Entity.Customer?.PaymentTermsDays ?? 14);
                return new
                {
                    invoice.Entity.CustomerId,
                    Name = invoice.Entity.Customer?.DisplayName ?? invoice.BuyerName,
                    Open = open,
                    Gross = invoice.GrossAmount,
                    Overdue = open > 0 && today > dueDate,
                };
            })
            .Where(x => x.Overdue)
            .GroupBy(x => new { x.CustomerId, x.Name })
            .Select(g => new ReportingCustomerPerformanceDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = string.IsNullOrWhiteSpace(g.Key.Name) ? "-" : g.Key.Name,
                RevenueGross = g.Sum(x => x.Gross),
                OpenAmount = g.Sum(x => x.Open),
                InvoiceCount = g.Count(),
            })
            .OrderByDescending(x => x.OpenAmount)
            .Take(5)
            .ToArray();

    private static ReportingDatevCoverageDto BuildDatevCoverage(IReadOnlyCollection<InvoiceProjection> invoices, IReadOnlyCollection<ExpenseEntity> expenses, IReadOnlyCollection<DatevExportLogEntity> datevLogs)
    {
        var invoicePeriods = datevLogs.Where(x => string.Equals(x.ExportType, "Invoices", StringComparison.OrdinalIgnoreCase)).ToArray();
        var expensePeriods = datevLogs.Where(x => string.Equals(x.ExportType, "Expenses", StringComparison.OrdinalIgnoreCase)).ToArray();

        var invoiceCount = invoices.Count;
        var exportedInvoiceCount = invoices.Count(invoice => invoicePeriods.Any(log => CoversDate(log, invoice.IssueDate)));
        var expenseCount = expenses.Count;
        var exportedExpenseCount = expenses.Count(expense => expensePeriods.Any(log => CoversDate(log, expense.IssueDate)));

        return new ReportingDatevCoverageDto
        {
            InvoiceCount = invoiceCount,
            ExportedInvoiceCount = exportedInvoiceCount,
            CoveragePercent = invoiceCount == 0 ? 0 : Math.Round((decimal)exportedInvoiceCount * 100 / invoiceCount, 1),
            ExpenseCount = expenseCount,
            ExportedExpenseCount = exportedExpenseCount,
            ExpenseCoveragePercent = expenseCount == 0 ? 0 : Math.Round((decimal)exportedExpenseCount * 100 / expenseCount, 1),
        };
    }

    private static bool CoversDate(DatevExportLogEntity log, DateOnly date)
    {
        if (log.PeriodFrom is null || log.PeriodTo is null) return false;
        return date >= log.PeriodFrom.Value && date <= log.PeriodTo.Value;
    }

    private static IReadOnlyList<ReportingExpenseCategoryDto> BuildExpenseCategories(IReadOnlyCollection<ExpenseEntity> expenses) =>
        expenses
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "General" : x.Category)
            .OrderByDescending(g => g.Sum(x => x.TotalAmount))
            .Select(g => new ReportingExpenseCategoryDto
            {
                Category = g.Key,
                NetAmount = g.Sum(x => x.NetAmount),
                TaxAmount = g.Sum(x => x.TaxAmount),
                TotalAmount = g.Sum(x => x.TotalAmount),
                Count = g.Count(),
            })
            .ToArray();

    private static InvoicePayload ParsePayload(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            var core = root.TryGetProperty("core", out var coreElement) ? coreElement : default;
            var buyer = root.TryGetProperty("buyer", out var buyerElement) ? buyerElement : default;

            var issueDate = ReadDateOnly(core, "BT-2");
            var taxAmount = ReadDecimal(core, "BT-110");
            var gross = ReadDecimal(core, "BT-112");
            var buyerName = ReadString(buyer, "BT-26");

            var lines = new List<LineProjection>();
            if (root.TryGetProperty("lines", out var linesElement) && linesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var lineElement in linesElement.EnumerateArray())
                {
                    var net = ReadDecimal(lineElement, "BT-131");
                    var rate = ReadDecimal(lineElement, "BT-151");
                    if (net == 0 && rate == 0) continue;
                    lines.Add(new LineProjection(net, rate));
                }
            }

            var netAmount = gross - taxAmount;
            if (netAmount <= 0 && lines.Count > 0)
            {
                netAmount = lines.Sum(line => line.NetAmount);
            }

            return new InvoicePayload(issueDate, netAmount, taxAmount, gross, buyerName, lines);
        }
        catch (JsonException)
        {
            return new InvoicePayload(DateOnly.MinValue, 0, 0, 0, "", new List<LineProjection>());
        }
    }

    private static string ReadString(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object) return "";
        return element.TryGetProperty(property, out var value) ? value.ToString() : "";
    }

    private static decimal ReadDecimal(JsonElement element, string property)
    {
        var text = ReadString(element, property);
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static DateOnly ReadDateOnly(JsonElement element, string property)
    {
        var text = ReadString(element, property);
        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : DateOnly.MinValue;
    }

    private sealed record InvoicePayload(DateOnly IssueDate, decimal NetAmount, decimal TaxAmount, decimal GrossAmount, string BuyerName, IReadOnlyList<LineProjection> Lines);

    private sealed record LineProjection(decimal NetAmount, decimal RatePercent);

    private sealed class InvoiceProjection
    {
        public InvoiceProjection(InvoiceDraftEntity entity, InvoicePayload payload)
        {
            Entity = entity;
            IssueDate = payload.IssueDate == DateOnly.MinValue ? DateOnly.FromDateTime(entity.CreatedAtUtc) : payload.IssueDate;
            NetAmount = payload.NetAmount;
            TaxAmount = payload.TaxAmount;
            GrossAmount = payload.GrossAmount;
            BuyerName = payload.BuyerName;
            Lines = payload.Lines;
        }

        public InvoiceDraftEntity Entity { get; }
        public DateOnly IssueDate { get; }
        public decimal NetAmount { get; }
        public decimal TaxAmount { get; }
        public decimal GrossAmount { get; }
        public string BuyerName { get; }
        public IReadOnlyList<LineProjection> Lines { get; }
    }
}
