using EasyMitt.Application.Dtos.Catalog;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductDto>> SearchAsync(Guid companyId, string? query, bool includeInactive, CancellationToken cancellationToken);

    Task<ProductDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(Guid companyId, ProductUpsertDto request, CancellationToken cancellationToken);

    Task<ProductDto?> UpdateAsync(Guid companyId, Guid id, ProductUpsertDto request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid companyId, Guid id, CancellationToken cancellationToken);
}
