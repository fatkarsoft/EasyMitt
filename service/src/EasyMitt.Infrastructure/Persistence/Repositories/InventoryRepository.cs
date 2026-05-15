using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Inventory;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository(EasyMittDbContext db) : IInventoryRepository
{
    public async Task<IReadOnlyList<InventoryMovementDto>> ListMovementsAsync(Guid companyId, Guid? productId, CancellationToken cancellationToken)
    {
        var rows = db.InventoryMovements
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.CompanyId == companyId);

        if (productId is { } id)
        {
            rows = rows.Where(x => x.ProductId == id);
        }

        return await rows
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<InventoryMovementDto?> CreateMovementAsync(Guid companyId, InventoryMovementCreateDto request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(
            x => x.CompanyId == companyId && x.Id == request.ProductId && x.IsActive,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        var entity = new InventoryMovementEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = product.Id,
            Product = product,
            Type = request.Type,
            QuantityDelta = request.QuantityDelta,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        product.CurrentStock += request.QuantityDelta;
        product.UpdatedAtUtc = DateTime.UtcNow;
        db.InventoryMovements.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static InventoryMovementDto ToDto(InventoryMovementEntity x) => new()
    {
        Id = x.Id,
        ProductId = x.ProductId,
        ProductName = x.Product?.Name ?? "",
        ProductSku = x.Product?.Sku ?? "",
        Type = x.Type,
        QuantityDelta = x.QuantityDelta,
        Reason = x.Reason,
        CreatedAtUtc = x.CreatedAtUtc,
    };
}
