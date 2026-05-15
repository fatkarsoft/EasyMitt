using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Datev;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class DatevSettingsRepository(EasyMittDbContext db) : IDatevSettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DatevSettingsDto> GetAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var entity = await db.DatevSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);

        return entity is null ? ToDto(Default(companyId)) : ToDto(entity);
    }

    public async Task<DatevSettingsDto> UpsertAsync(Guid companyId, DatevSettingsUpsertDto request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = await db.DatevSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        if (entity is null)
        {
            entity = Default(companyId);
            entity.CreatedAtUtc = now;
            db.DatevSettings.Add(entity);
        }

        entity.ExportFormat = request.ExportFormat is "DatevExtf" ? "DatevExtf" : "BasicCsv";
        entity.ChartOfAccounts = request.ChartOfAccounts is "SKR04" ? "SKR04" : "SKR03";
        entity.RevenueAccount = Clean(request.RevenueAccount, "8400");
        entity.DefaultExpenseAccount = Clean(request.DefaultExpenseAccount, "4980");
        entity.CustomerContraAccount = Clean(request.CustomerContraAccount, "10000");
        entity.VendorContraAccount = Clean(request.VendorContraAccount, "70000");
        entity.ConsultantNumber = TrimOrNull(request.ConsultantNumber);
        entity.ClientNumber = TrimOrNull(request.ClientNumber);
        entity.FiscalYearStart = request.FiscalYearStart == default ? new DateOnly(DateTime.UtcNow.Year, 1, 1) : request.FiscalYearStart;
        entity.ExpenseAccountMappingsJson = JsonSerializer.Serialize(
            request.ExpenseAccountMappings
                .Where(x => !string.IsNullOrWhiteSpace(x.Category) && !string.IsNullOrWhiteSpace(x.Account))
                .Select(x => new DatevExpenseAccountMappingDto
                {
                    Category = x.Category.Trim(),
                    Account = x.Account.Trim(),
                })
                .GroupBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Last())
                .OrderBy(x => x.Category)
                .ToArray(),
            JsonOptions);
        entity.TaxKeyMappingsJson = JsonSerializer.Serialize(
            NormalizeTaxKeyMappings(request.TaxKeyMappings),
            JsonOptions);
        entity.UpdatedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static DatevSettingsEntity Default(Guid companyId)
    {
        var now = DateTime.UtcNow;
        return new DatevSettingsEntity
        {
            CompanyId = companyId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            TaxKeyMappingsJson = JsonSerializer.Serialize(DefaultTaxKeyMappings(), JsonOptions),
        };
    }

    private static string Clean(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DatevSettingsDto ToDto(DatevSettingsEntity entity) => new()
    {
        ExportFormat = entity.ExportFormat is "DatevExtf" ? "DatevExtf" : "BasicCsv",
        ChartOfAccounts = entity.ChartOfAccounts,
        RevenueAccount = entity.RevenueAccount,
        DefaultExpenseAccount = entity.DefaultExpenseAccount,
        CustomerContraAccount = entity.CustomerContraAccount,
        VendorContraAccount = entity.VendorContraAccount,
        ConsultantNumber = entity.ConsultantNumber,
        ClientNumber = entity.ClientNumber,
        FiscalYearStart = entity.FiscalYearStart,
        ExpenseAccountMappings = ReadMappings(entity.ExpenseAccountMappingsJson),
        TaxKeyMappings = ReadTaxKeyMappings(entity.TaxKeyMappingsJson),
        UpdatedAtUtc = entity.UpdatedAtUtc,
    };

    private static IReadOnlyList<DatevExpenseAccountMappingDto> ReadMappings(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<DatevExpenseAccountMappingDto>>(json, JsonOptions)
                ?? Array.Empty<DatevExpenseAccountMappingDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<DatevExpenseAccountMappingDto>();
        }
    }

    private static IReadOnlyList<DatevTaxKeyMappingDto> ReadTaxKeyMappings(string json)
    {
        try
        {
            var mappings = JsonSerializer.Deserialize<IReadOnlyList<DatevTaxKeyMappingDto>>(json, JsonOptions);
            return mappings is { Count: > 0 } ? mappings : DefaultTaxKeyMappings();
        }
        catch (JsonException)
        {
            return DefaultTaxKeyMappings();
        }
    }

    private static IReadOnlyList<DatevTaxKeyMappingDto> NormalizeTaxKeyMappings(IReadOnlyList<DatevTaxKeyMappingDto> mappings)
    {
        var normalized = mappings
            .Where(x => x.Source is "Invoice" or "Expense")
            .Select(x => new DatevTaxKeyMappingDto
            {
                Source = x.Source,
                VatRate = decimal.Round(Math.Max(0, x.VatRate), 2),
                TaxKey = x.TaxKey.Trim(),
            })
            .GroupBy(x => $"{x.Source}:{x.VatRate}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last())
            .OrderBy(x => x.Source)
            .ThenBy(x => x.VatRate)
            .ToArray();

        return normalized.Length > 0 ? normalized : DefaultTaxKeyMappings();
    }

    private static IReadOnlyList<DatevTaxKeyMappingDto> DefaultTaxKeyMappings() => new[]
    {
        new DatevTaxKeyMappingDto { Source = "Invoice", VatRate = 19, TaxKey = "3" },
        new DatevTaxKeyMappingDto { Source = "Invoice", VatRate = 7, TaxKey = "2" },
        new DatevTaxKeyMappingDto { Source = "Invoice", VatRate = 0, TaxKey = "" },
        new DatevTaxKeyMappingDto { Source = "Expense", VatRate = 19, TaxKey = "9" },
        new DatevTaxKeyMappingDto { Source = "Expense", VatRate = 7, TaxKey = "8" },
        new DatevTaxKeyMappingDto { Source = "Expense", VatRate = 0, TaxKey = "" },
    };
}
