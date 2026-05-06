using EasyMitt.Application.Abstractions.Communication;

namespace EasyMitt.Infrastructure.Communication;

public sealed class NoOpInvoiceDispatch : IInvoiceDispatch
{
    public Task<InvoiceDispatchReceipt> SubmitAsync(InvoiceDispatchRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        var id = $"noop-{Guid.NewGuid():N}";
        IReadOnlyDictionary<string, string> meta = new Dictionary<string, string>
        {
            ["channel"] = "none",
            ["detail"] = "Peppol erişim noktası henüz yapılandırılmadı.",
        };

        return Task.FromResult(new InvoiceDispatchReceipt(id, "accepted_stub", meta));
    }
}
