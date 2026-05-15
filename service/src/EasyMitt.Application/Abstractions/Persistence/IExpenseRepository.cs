using EasyMitt.Application.Dtos.Expenses;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IExpenseRepository
{
    Task<IReadOnlyList<ExpenseDto>> SearchAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken);
    Task<ExpenseDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);
    Task<ExpenseDto> CreateAsync(Guid companyId, ExpenseUpsertDto request, CancellationToken cancellationToken);
    Task<ExpenseDto?> UpdateAsync(Guid companyId, Guid id, ExpenseUpsertDto request, CancellationToken cancellationToken);
    Task<ExpenseDto?> UpdateStatusAsync(Guid companyId, Guid id, string status, CancellationToken cancellationToken);
}
