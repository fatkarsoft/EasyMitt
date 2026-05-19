using EasyMitt.Application.Abstractions.Compliance;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Domain.Compliance;

namespace EasyMitt.Infrastructure.ElectronicInvoicing;

public sealed class InvoiceSchematronValidator : IInvoiceSchematronValidator
{
    public SchematronValidationResult Validate(InvoiceDocumentDto document)
    {
        if (document is null)
            return new SchematronValidationResult(false, new[]
            {
                new SchematronFailure("BR-02", "fatal", "Invoice document is missing.", null),
            });

        var input = new XRechnungSchematronRules.RuleInput(
            InvoiceNumber: document.Core?.InvoiceNumber ?? "",
            IssueDate: document.Core?.IssueDate ?? default,
            CurrencyCode: document.Core?.CurrencyCode ?? "",
            BuyerReference: document.Core?.BuyerReference ?? "",
            TaxAmount: document.Core?.TaxAmount ?? 0m,
            InvoiceTotalVatIncluded: document.Core?.InvoiceTotalVatIncluded ?? 0m,
            SellerName: document.Seller?.Name ?? "",
            SellerVatId: document.Seller?.VatId,
            SellerIban: document.Seller?.PaymentIban,
            BuyerName: document.Buyer?.Name ?? "",
            BuyerVatId: document.Buyer?.VatId,
            LineCount: document.Lines?.Count ?? 0,
            LinesNetSum: document.Lines?.Sum(l => l.NetAmount) ?? 0m,
            MaxLineVatRatePercent: document.Lines?.Count > 0 ? document.Lines.Max(l => l.VatRatePercent) : 0m);

        var failures = XRechnungSchematronRules.Evaluate(input);
        var mapped = failures
            .Select(f => new SchematronFailure(f.RuleId, f.Severity, f.Description, f.Field))
            .ToArray();

        var hasFatal = mapped.Any(f => string.Equals(f.Severity, "fatal", StringComparison.OrdinalIgnoreCase));
        return new SchematronValidationResult(!hasFatal, mapped);
    }
}
