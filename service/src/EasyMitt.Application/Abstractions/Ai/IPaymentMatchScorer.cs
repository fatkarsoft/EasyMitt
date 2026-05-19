using EasyMitt.Application.Dtos.Ai;

namespace EasyMitt.Application.Abstractions.Ai;

public interface IPaymentMatchScorer
{
    Task<IReadOnlyList<PaymentMatchSuggestionDto>> SuggestAsync(
        Guid companyId,
        Guid bankTransactionId,
        CancellationToken cancellationToken);
}
