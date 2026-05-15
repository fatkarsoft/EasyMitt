using EasyMitt.Application.Dtos.Inventory;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventoryMovementDto>> ListMovementsAsync(Guid companyId, Guid? productId, CancellationToken cancellationToken);

    Task<InventoryMovementDto?> CreateMovementAsync(Guid companyId, InventoryMovementCreateDto request, CancellationToken cancellationToken);
}
