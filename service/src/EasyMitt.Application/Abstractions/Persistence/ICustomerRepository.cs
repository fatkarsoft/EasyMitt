using EasyMitt.Application.Dtos.Customers;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerDto>> SearchAsync(Guid companyId, string? query, bool includeInactive, CancellationToken cancellationToken);

    Task<CustomerDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);

    Task<CustomerDto> CreateAsync(Guid companyId, CustomerUpsertDto request, CancellationToken cancellationToken);

    Task<CustomerDto?> UpdateAsync(Guid companyId, Guid id, CustomerUpsertDto request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid companyId, Guid id, CancellationToken cancellationToken);
}
