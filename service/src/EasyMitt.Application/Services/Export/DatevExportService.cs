using System.Globalization;
using System.Text;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Export;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Customers;
using EasyMitt.Application.Dtos.Datev;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Dtos.Expenses;
using EasyMitt.Domain.Billing;

namespace EasyMitt.Application.Services.Export;

public sealed class DatevExportService : IDatevExportService
{
    private const string ContentType = "text/csv; charset=utf-8";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly HashSet<string> ExportableInvoiceStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        InvoiceLifecycleStatus.Issued,
        InvoiceLifecycleStatus.Sent,
        InvoiceLifecycleStatus.Paid,
        InvoiceLifecycleStatus.Overdue,
    };

    private readonly IInvoiceDraftRepository invoiceDraftRepository;
    private readonly IExpenseRepository expenseRepository;
    private readonly IDatevSettingsRepository datevSettingsRepository;
    private readonly ICustomerRepository customerRepository;

    public DatevExportService(
        IInvoiceDraftRepository invoiceDraftRepository,
        IExpenseRepository expenseRepository,
        IDatevSettingsRepository datevSettingsRepository,
        ICustomerRepository customerRepository)
    {
        this.invoiceDraftRepository = invoiceDraftRepository;
        this.expenseRepository = expenseRepository;
        this.datevSettingsRepository = datevSettingsRepository;
        this.customerRepository = customerRepository;
    }

    public async Task<DatevExportFile> ExportInvoicesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var settings = await datevSettingsRepository.GetAsync(companyId, cancellationToken);
        var rows = await BuildInvoiceRowsAsync(companyId, status, from, to, cancellationToken);
        return BuildFile("datev-invoices", settings, rows, from, to);
    }

    public async Task<DatevExportFile> ExportExpensesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var settings = await datevSettingsRepository.GetAsync(companyId, cancellationToken);
        var rows = await BuildExpenseRowsAsync(companyId, status, from, to, cancellationToken);
        return BuildFile("datev-expenses", settings, rows, from, to);
    }

    public async Task<DatevExportPreviewDto> PreviewInvoicesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var settings = await datevSettingsRepository.GetAsync(companyId, cancellationToken);
        var rows = await BuildInvoiceRowsAsync(companyId, status, from, to, cancellationToken);
        return BuildPreview(settings, rows, from, to);
    }

    public async Task<DatevExportPreviewDto> PreviewExpensesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var settings = await datevSettingsRepository.GetAsync(companyId, cancellationToken);
        var rows = await BuildExpenseRowsAsync(companyId, status, from, to, cancellationToken);
        return BuildPreview(settings, rows, from, to);
    }

    private async Task<IReadOnlyList<DatevExportRow>> BuildInvoiceRowsAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var settings = await datevSettingsRepository.GetAsync(companyId, cancellationToken);
        var normalizedStatus = NormalizeStatus(status);
        var records = await invoiceDraftRepository.ListAsync(companyId, null, normalizedStatus, cancellationToken);
        var rows = records
            .Where(record => normalizedStatus is not null || ExportableInvoiceStatuses.Contains(record.Status))
            .Select(async record => MapInvoice(
                record,
                settings,
                record.CustomerId.HasValue
                    ? await customerRepository.GetAsync(companyId, record.CustomerId.Value, cancellationToken)
                    : null))
            .ToArray();

        return (await Task.WhenAll(rows)).Where(row => IsInRange(row.DocumentDate, from, to)).ToArray();
    }

    private async Task<IReadOnlyList<DatevExportRow>> BuildExpenseRowsAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var settings = await datevSettingsRepository.GetAsync(companyId, cancellationToken);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Booked" : NormalizeStatus(status);
        var expenses = await expenseRepository.SearchAsync(companyId, null, normalizedStatus, cancellationToken);
        return expenses.Select(expense => MapExpense(expense, settings)).Where(row => IsInRange(row.DocumentDate, from, to)).ToArray();
    }

    private static DatevExportRow MapInvoice(InvoiceDraftRecord record, DatevSettingsDto settings, CustomerDto? customer)
    {
        var document = TryDeserializeInvoice(record.PayloadJson);
        var core = document?.Core;
        var usesDefaultCustomerAccount = string.IsNullOrWhiteSpace(customer?.DatevDebitorAccount);
        var vatRate = InferVatRate(core?.InvoiceTotalVatIncluded ?? 0, core?.TaxAmount ?? 0);
        var taxKey = TaxKeyFor("Invoice", vatRate, settings);

        return new DatevExportRow(
            core?.IssueDate ?? DateOnly.FromDateTime(record.CreatedAtUtc),
            core?.InvoiceNumber ?? record.Id.ToString("N")[..8],
            document?.Buyer.Name ?? "",
            settings.RevenueAccount,
            usesDefaultCustomerAccount ? settings.CustomerContraAccount : customer!.DatevDebitorAccount!,
            "H",
            core?.InvoiceTotalVatIncluded ?? 0,
            core?.TaxAmount ?? 0,
            vatRate,
            taxKey,
            core?.CurrencyCode ?? "EUR",
            "Invoice",
            record.Status,
            BuildWarnings(
                (core is null, "invoice_payload_unreadable"),
                (string.IsNullOrWhiteSpace(core?.InvoiceNumber), "missing_document_number"),
                (string.IsNullOrWhiteSpace(document?.Buyer.Name), "missing_booking_text"),
                (string.IsNullOrWhiteSpace(settings.RevenueAccount), "missing_account"),
                (string.IsNullOrWhiteSpace(settings.CustomerContraAccount), "missing_offset_account"),
                (usesDefaultCustomerAccount, "customer_uses_default_account"),
                (vatRate > 0 && string.IsNullOrWhiteSpace(taxKey), "missing_tax_key"),
                ((core?.InvoiceTotalVatIncluded ?? 0) <= 0, "amount_not_positive")));
    }

    private static DatevExportRow MapExpense(ExpenseDto expense, DatevSettingsDto settings)
    {
        var usesDefaultVendorAccount = string.IsNullOrWhiteSpace(expense.DatevCreditorAccount);
        var vatRate = InferVatRate(expense.TotalAmount, expense.TaxAmount);
        var taxKey = TaxKeyFor("Expense", vatRate, settings);

        return new DatevExportRow(
            expense.IssueDate,
            expense.DocumentNumber ?? expense.Id.ToString("N")[..8],
            $"{expense.VendorName} {expense.Category}".Trim(),
            ExpenseAccountFor(expense.Category, settings),
            usesDefaultVendorAccount ? settings.VendorContraAccount : expense.DatevCreditorAccount!,
            "S",
            expense.TotalAmount,
            expense.TaxAmount,
            vatRate,
            taxKey,
            expense.CurrencyCode,
            "Expense",
            expense.Status,
            BuildWarnings(
                (string.IsNullOrWhiteSpace(expense.DocumentNumber), "missing_document_number"),
                (string.IsNullOrWhiteSpace(expense.VendorName), "missing_booking_text"),
                (UsesDefaultExpenseAccount(expense.Category, settings), "category_uses_default_account"),
                (string.IsNullOrWhiteSpace(settings.VendorContraAccount), "missing_offset_account"),
                (usesDefaultVendorAccount, "vendor_uses_default_account"),
                (vatRate > 0 && string.IsNullOrWhiteSpace(taxKey), "missing_tax_key"),
                (expense.TotalAmount <= 0, "amount_not_positive")));
    }

    private static string ExpenseAccountFor(string category, DatevSettingsDto settings)
    {
        var match = settings.ExpenseAccountMappings.FirstOrDefault(x =>
            x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match?.Account) ? settings.DefaultExpenseAccount : match.Account;
    }

    private static bool UsesDefaultExpenseAccount(string category, DatevSettingsDto settings) =>
        !settings.ExpenseAccountMappings.Any(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    private static string? TaxKeyFor(string source, decimal vatRate, DatevSettingsDto settings)
    {
        var match = settings.TaxKeyMappings.FirstOrDefault(x =>
            x.Source.Equals(source, StringComparison.OrdinalIgnoreCase) &&
            x.VatRate == vatRate);
        return string.IsNullOrWhiteSpace(match?.TaxKey) ? null : match.TaxKey;
    }

    private static IReadOnlyList<string> BuildWarnings(params (bool HasWarning, string Code)[] warnings) =>
        warnings.Where(x => x.HasWarning).Select(x => x.Code).ToArray();

    private static bool IsInRange(DateOnly date, DateOnly? from, DateOnly? to) =>
        (!from.HasValue || date >= from.Value) && (!to.HasValue || date <= to.Value);

    private static DatevExportFile BuildFile(string prefix, DatevSettingsDto settings, IReadOnlyCollection<DatevExportRow> rows, DateOnly? from, DateOnly? to) =>
        settings.ExportFormat is "DatevExtf"
            ? BuildExtfFile(prefix, settings, rows, from, to)
            : BuildBasicFile(prefix, rows);

    private static DatevExportFile BuildBasicFile(string prefix, IReadOnlyCollection<DatevExportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Belegdatum;Belegnummer;Buchungstext;Konto;Gegenkonto;Soll/Haben;Umsatz;Steuer;Waehrung;Quelle;Status");

        foreach (var row in rows.OrderBy(row => row.DocumentDate).ThenBy(row => row.DocumentNumber, StringComparer.OrdinalIgnoreCase))
        {
            builder
                .Append(Escape(row.DocumentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append(';')
                .Append(Escape(row.DocumentNumber)).Append(';')
                .Append(Escape(row.BookingText)).Append(';')
                .Append(Escape(row.Account)).Append(';')
                .Append(Escape(row.OffsetAccount)).Append(';')
                .Append(Escape(row.DebitCredit)).Append(';')
                .Append(Escape(FormatAmount(row.Amount))).Append(';')
                .Append(Escape(FormatAmount(row.TaxAmount))).Append(';')
                .Append(Escape(row.CurrencyCode)).Append(';')
                .Append(Escape(row.Source)).Append(';')
                .Append(Escape(row.Status))
                .AppendLine();
        }

        return new DatevExportFile(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(),
            $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv",
            ContentType,
            rows.Count,
            rows.Count(row => row.Warnings.Count > 0),
            rows.Sum(row => row.Amount),
            rows.Sum(row => row.TaxAmount));
    }

    private static DatevExportFile BuildExtfFile(string prefix, DatevSettingsDto settings, IReadOnlyCollection<DatevExportRow> rows, DateOnly? from, DateOnly? to)
    {
        var builder = new StringBuilder();
        var periodFrom = (from ?? settings.FiscalYearStart).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var periodTo = (to ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        builder.Append("EXTF;700;21;Buchungsstapel;13;EasyMitt;");
        builder.Append(Escape(settings.ConsultantNumber)).Append(';');
        builder.Append(Escape(settings.ClientNumber)).Append(';');
        builder.Append(Escape(settings.FiscalYearStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture))).Append(';');
        builder.Append(Escape(periodFrom)).Append(';');
        builder.Append(Escape(periodTo)).AppendLine(";");
        builder.AppendLine("Umsatz;Soll/Haben-Kennzeichen;WKZ Umsatz;Konto;Gegenkonto;Belegdatum;Belegfeld 1;Buchungstext;Steuerschlüssel;Steuersatz;EU-Land u. UStID;Kost1;Kost2");

        foreach (var row in rows.OrderBy(row => row.DocumentDate).ThenBy(row => row.DocumentNumber, StringComparer.OrdinalIgnoreCase))
        {
            builder
                .Append(Escape(FormatDatevAmount(row.Amount))).Append(';')
                .Append(Escape(row.DebitCredit)).Append(';')
                .Append(Escape(row.CurrencyCode)).Append(';')
                .Append(Escape(row.Account)).Append(';')
                .Append(Escape(row.OffsetAccount)).Append(';')
                .Append(Escape(row.DocumentDate.ToString("ddMM", CultureInfo.InvariantCulture))).Append(';')
                .Append(Escape(row.DocumentNumber)).Append(';')
                .Append(Escape(row.BookingText)).Append(';')
                .Append(Escape(row.TaxKey)).Append(';')
                .Append(Escape(row.VatRate.ToString("0.##", CultureInfo.InvariantCulture))).Append(';')
                .Append(';')
                .Append(';')
                .AppendLine();
        }

        return new DatevExportFile(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(),
            $"{prefix}-extf-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv",
            ContentType,
            rows.Count,
            rows.Count(row => row.Warnings.Count > 0),
            rows.Sum(row => row.Amount),
            rows.Sum(row => row.TaxAmount));
    }

    private static DatevExportPreviewDto BuildPreview(DatevSettingsDto settings, IReadOnlyCollection<DatevExportRow> rows, DateOnly? from, DateOnly? to)
    {
        var ordered = rows.OrderBy(row => row.DocumentDate).ThenBy(row => row.DocumentNumber, StringComparer.OrdinalIgnoreCase).ToArray();
        return new DatevExportPreviewDto
        {
            ExportFormat = settings.ExportFormat,
            From = from,
            To = to,
            Rows = ordered.Select(row => new DatevExportPreviewRowDto
            {
                DocumentDate = row.DocumentDate,
                DocumentNumber = row.DocumentNumber,
                BookingText = row.BookingText,
                Account = row.Account,
                OffsetAccount = row.OffsetAccount,
                DebitCredit = row.DebitCredit,
                Amount = row.Amount,
                TaxAmount = row.TaxAmount,
                VatRate = row.VatRate,
                TaxKey = row.TaxKey,
                CurrencyCode = row.CurrencyCode,
                Source = row.Source,
                Status = row.Status,
                Warnings = row.Warnings,
            }).ToArray(),
            ReadyCount = ordered.Count(row => row.Warnings.Count == 0),
            WarningCount = ordered.Count(row => row.Warnings.Count > 0),
            TotalAmount = ordered.Sum(row => row.Amount),
            TotalTaxAmount = ordered.Sum(row => row.TaxAmount),
        };
    }

    private static InvoiceDocumentDto? TryDeserializeInvoice(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<InvoiceDocumentDto>(payloadJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : status.Trim();

    private static string FormatAmount(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDatevAmount(decimal value) =>
        value.ToString("0.00", CultureInfo.GetCultureInfo("de-DE"));

    private static decimal InferVatRate(decimal amount, decimal taxAmount)
    {
        if (amount <= 0 || taxAmount <= 0)
        {
            return 0;
        }

        var net = amount - taxAmount;
        if (net <= 0)
        {
            return 0;
        }

        return decimal.Round((taxAmount / net) * 100, 0);
    }

    private static string Escape(string? value)
    {
        var text = value ?? "";
        return text.Contains(';') || text.Contains('"') || text.Contains('\n') || text.Contains('\r')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }

    private sealed record DatevExportRow(
        DateOnly DocumentDate,
        string DocumentNumber,
        string BookingText,
        string Account,
        string OffsetAccount,
        string DebitCredit,
        decimal Amount,
        decimal TaxAmount,
        decimal VatRate,
        string? TaxKey,
        string CurrencyCode,
        string Source,
        string Status,
        IReadOnlyList<string> Warnings);
}
