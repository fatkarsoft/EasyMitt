using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Catalog;
using EasyMitt.Domain.Taxation;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(EasyMittDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<ProductDto>> SearchAsync(Guid companyId, string? query, bool includeInactive, CancellationToken cancellationToken)
    {
        var normalized = query?.Trim().ToLowerInvariant();
        var rows = db.Products.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!includeInactive)
        {
            rows = rows.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            rows = rows.Where(x =>
                x.Name.ToLower().Contains(normalized) ||
                x.Sku.ToLower().Contains(normalized));
        }

        return await rows.OrderBy(x => x.Name).Take(100).Select(x => ToDto(x)).ToArrayAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ProductDto> CreateAsync(Guid companyId, ProductUpsertDto request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new ProductEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        Apply(entity, request);
        db.Products.Add(entity);

        if (entity.CurrentStock != 0)
        {
            db.InventoryMovements.Add(new InventoryMovementEntity
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ProductId = entity.Id,
                Type = "OpeningBalance",
                QuantityDelta = entity.CurrentStock,
                Reason = "Initial stock",
                CreatedAtUtc = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ProductDto?> UpdateAsync(Guid companyId, Guid id, ProductUpsertDto request, CancellationToken cancellationToken)
    {
        var entity = await db.Products.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        Apply(entity, request, preserveStock: true);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Products.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(ProductEntity entity, ProductUpsertDto request, bool preserveStock = false)
    {
        entity.Type = request.Type is "Service" ? "Service" : "Product";
        entity.Sku = request.Sku.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = TrimOrNull(request.Description);
        entity.Unit = string.IsNullOrWhiteSpace(request.Unit) ? "pcs" : request.Unit.Trim();
        entity.NetPrice = Math.Max(0, request.NetPrice);
        entity.VatRatePercent = GermanVatRatePolicy.NormalizeOrDefault(request.VatRatePercent);
        if (!preserveStock)
        {
            entity.CurrentStock = Math.Max(0, request.CurrentStock);
        }

        entity.MinimumStock = Math.Max(0, request.MinimumStock);
        entity.IsActive = request.IsActive;
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProductDto ToDto(ProductEntity x) => new()
    {
        Id = x.Id,
        Type = x.Type,
        Sku = x.Sku,
        Name = x.Name,
        Description = x.Description,
        Unit = x.Unit,
        NetPrice = x.NetPrice,
        VatRatePercent = x.VatRatePercent,
        CurrentStock = x.CurrentStock,
        MinimumStock = x.MinimumStock,
        IsActive = x.IsActive,
        CreatedAtUtc = x.CreatedAtUtc,
        UpdatedAtUtc = x.UpdatedAtUtc,
    };
}
