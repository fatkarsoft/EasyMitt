using EasyMitt.Application.Dtos.Ingestion;

namespace EasyMitt.Application.Abstractions.Transformation;

public interface IScannedInvoiceImportAnalyzer
{
    Task<RawInvoiceImportDto> AnalyzeAsync(
        Stream file,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}
