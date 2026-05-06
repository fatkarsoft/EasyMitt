using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace EasyMitt.Application.Services.Billing;

public sealed class InvoiceDraftWorkflow(
    IValidator<InvoiceDocumentDto> documentValidator,
    IInvoiceDraftRepository draftRepository,
    IImmutableArchiveStore archiveStore) : IInvoiceDraftWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public Task<ValidationResult> ValidateAsync(
        InvoiceDocumentDto document,
        CancellationToken cancellationToken) =>
        documentValidator.ValidateAsync(document, cancellationToken);

    public async Task<Guid> SaveDraftAsync(InvoiceDocumentDto document, CancellationToken cancellationToken)
    {
        var validation = await documentValidator.ValidateAsync(document, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var json = JsonSerializer.Serialize(document, JsonOptions);
        var hashHex = Sha256Hex(Encoding.UTF8.GetBytes(json));
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var archive = await archiveStore.WriteAsync(Encoding.UTF8.GetBytes(json), hashHex, cancellationToken);

        var record = new InvoiceDraftRecord(
            id,
            json,
            hashHex,
            now,
            now,
            IsImmutableSnapshot: true,
            ArchiveObjectKey: archive.ObjectKey);

        await draftRepository.InsertAsync(record, cancellationToken);
        return id;
    }

    private static string Sha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
