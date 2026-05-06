using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Abstractions.Transformation;

/// <summary>
/// EN 16931 tabanlı fatura çıktıları (XRechnung CII XML, ZUGFeRD PDF/A-3).
/// </summary>
public interface IElectronicInvoiceGenerator
{
    Task<byte[]> GenerateXRechnungXmlAsync(InvoiceDocumentDto document, CancellationToken cancellationToken);

    /// <summary>
    /// Görsel PDF gövdesi üzerine ZUGFeRD 2.3 XML gömülü PDF/A-3 üretir.
    /// <paramref name="visualPdf"/> null ise sunucu varsayılan boş şablonu kullanır.
    /// </summary>
    Task<byte[]> GenerateZugferdPdfAsync(
        InvoiceDocumentDto document,
        Stream? visualPdf,
        CancellationToken cancellationToken);
}
