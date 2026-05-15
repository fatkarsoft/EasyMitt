using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Customers;
using EasyMitt.Domain.Germany;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(EasyMittDbContext db) : ICustomerRepository
{
    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(Guid companyId, string? query, bool includeInactive, CancellationToken cancellationToken)
    {
        var normalized = query?.Trim().ToLowerInvariant();
        var rows = db.Customers.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!includeInactive)
        {
            rows = rows.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            rows = rows.Where(x =>
                x.DisplayName.ToLower().Contains(normalized) ||
                (x.Email != null && x.Email.ToLower().Contains(normalized)) ||
                (x.VatId != null && x.VatId.ToLower().Contains(normalized)));
        }

        return await rows.OrderBy(x => x.DisplayName).Take(100).Select(x => ToDto(x)).ToArrayAsync(cancellationToken);
    }

    public async Task<CustomerDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken) =>
        await db.Customers.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CustomerDto> CreateAsync(Guid companyId, CustomerUpsertDto request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        Apply(entity, request);
        db.Customers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid companyId, Guid id, CustomerUpsertDto request, CancellationToken cancellationToken)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        Apply(entity, request);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(CustomerEntity entity, CustomerUpsertDto request)
    {
        entity.Type = request.Type is "Consumer" ? "Consumer" : "Business";
        entity.CompanyName = entity.Type == "Business" ? TrimOrNull(request.CompanyName) : null;
        entity.FirstName = entity.Type == "Consumer" ? TrimOrNull(request.FirstName) : null;
        entity.LastName = entity.Type == "Consumer" ? TrimOrNull(request.LastName) : null;
        entity.DisplayName = BuildDisplayName(entity);
        entity.Email = TrimOrNull(request.Email)?.ToLowerInvariant();
        entity.Phone = TrimOrNull(request.Phone);
        entity.Street = TrimOrNull(request.Street);
        entity.PostalCode = TrimOrNull(request.PostalCode);
        entity.City = TrimOrNull(request.City);
        entity.CountryCode = GermanCountryPolicy.NormalizeCountryCode(request.CountryCode);
        entity.VatId = entity.Type == "Business" ? TrimOrNull(request.VatId)?.ToUpperInvariant() : null;
        entity.TaxNumber = entity.Type == "Business" ? TrimOrNull(request.TaxNumber) : null;
        entity.LeitwegId = entity.Type == "Business" ? TrimOrNull(request.LeitwegId) : null;
        entity.DatevDebitorAccount = TrimOrNull(request.DatevDebitorAccount);
        entity.PaymentTermsDays = Math.Max(0, request.PaymentTermsDays);
        entity.Notes = TrimOrNull(request.Notes);
        entity.IsActive = request.IsActive;
    }

    private static string BuildDisplayName(CustomerEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.CompanyName))
        {
            return entity.CompanyName;
        }

        return string.Join(' ', new[] { entity.FirstName, entity.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CustomerDto ToDto(CustomerEntity x) => new()
    {
        Id = x.Id,
        Type = x.Type,
        DisplayName = x.DisplayName,
        CompanyName = x.CompanyName,
        FirstName = x.FirstName,
        LastName = x.LastName,
        Email = x.Email,
        Phone = x.Phone,
        Street = x.Street,
        PostalCode = x.PostalCode,
        City = x.City,
        CountryCode = x.CountryCode,
        VatId = x.VatId,
        TaxNumber = x.TaxNumber,
        LeitwegId = x.LeitwegId,
        DatevDebitorAccount = x.DatevDebitorAccount,
        PaymentTermsDays = x.PaymentTermsDays,
        Notes = x.Notes,
        IsActive = x.IsActive,
        CreatedAtUtc = x.CreatedAtUtc,
        UpdatedAtUtc = x.UpdatedAtUtc,
    };
}
