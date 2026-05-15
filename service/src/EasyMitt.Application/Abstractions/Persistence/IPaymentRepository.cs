using EasyMitt.Application.Dtos.Payments;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<IReadOnlyList<BankTransactionDto>> SearchTransactionsAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken);

    Task<BankTransactionDto?> GetTransactionAsync(Guid companyId, Guid id, CancellationToken cancellationToken);

    Task<BankTransactionDto> CreateTransactionAsync(Guid companyId, BankTransactionCreateDto request, CancellationToken cancellationToken);

    Task<IReadOnlyList<BankTransactionDto>> ImportTransactionsAsync(Guid companyId, IReadOnlyList<BankTransactionCreateDto> requests, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentSuggestionDto>> SuggestInvoicesAsync(Guid companyId, Guid bankTransactionId, CancellationToken cancellationToken);

    Task<PaymentAllocationDto?> AllocateAsync(Guid companyId, PaymentAllocationCreateDto request, CancellationToken cancellationToken);

    Task<InvoicePaymentSummaryDto?> GetInvoiceSummaryAsync(Guid companyId, Guid invoiceDraftId, CancellationToken cancellationToken);
}
