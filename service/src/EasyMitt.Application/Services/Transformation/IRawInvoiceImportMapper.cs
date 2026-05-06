using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Dtos.Ingestion;

namespace EasyMitt.Application.Services.Transformation;

public interface IRawInvoiceImportMapper
{
    InvoiceDocumentDto MapFromRaw(RawInvoiceImportDto raw);
}
