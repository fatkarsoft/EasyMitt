using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Dtos.Ingestion;
using EasyMitt.Domain.Taxation;

namespace EasyMitt.Application.Services.Transformation;

/// <summary>
/// Ham AI çıktısından EN 16931 DTO eşlemesi (Studio: adapter / mapping).
/// </summary>
public sealed class RawInvoiceImportMapper : IRawInvoiceImportMapper
{
    public InvoiceDocumentDto MapFromRaw(RawInvoiceImportDto raw)
    {
        var issue = raw.IssueDateHint ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var currency = string.IsNullOrWhiteSpace(raw.CurrencyHint) ? "EUR" : raw.CurrencyHint!.Trim().ToUpperInvariant();
        var total = raw.TotalAmount ?? 0m;

        var lines = raw.LineHints.Count > 0
            ? raw.LineHints.Select((l, i) => new InvoiceLineDto
            {
                ItemName = string.IsNullOrWhiteSpace(l.Description) ? $"Satır {i + 1}" : l.Description!,
                Quantity = 1,
                NetAmount = l.Amount ?? 0m,
                VatRatePercent = GermanVatRatePolicy.NormalizeOrDefault(l.VatRatePercent),
            }).ToList()
            : new List<InvoiceLineDto>
            {
                new()
                {
                    ItemName = "Genel hizmet",
                    Quantity = 1,
                    NetAmount = total,
                    VatRatePercent = GermanVatRatePolicy.NormalizeOrDefault(null),
                },
            };

        var taxAmount = lines.Sum(line => decimal.Round(
            line.NetAmount * line.VatRatePercent / 100m,
            2,
            MidpointRounding.AwayFromZero));

        return new InvoiceDocumentDto
        {
            Core = new InvoiceCoreDto
            {
                InvoiceNumber = $"DRAFT-{Guid.NewGuid():N}"[..16],
                IssueDate = issue,
                CurrencyCode = currency,
                BuyerReference = string.IsNullOrWhiteSpace(raw.BuyerReferenceHint) ? "" : raw.BuyerReferenceHint!,
                TaxAmount = taxAmount,
                InvoiceTotalVatIncluded = total > 0 ? total : lines.Sum(line => line.NetAmount) + taxAmount,
            },
            Seller = new SellerPartyDto
            {
                Name = string.IsNullOrWhiteSpace(raw.MerchantOrSellerHint) ? "Satıcı (AI)" : raw.MerchantOrSellerHint!,
                VatId = raw.SellerVatIdHint,
                PaymentIban = raw.IbanOrPaymentHint,
            },
            Buyer = new BuyerPartyDto
            {
                Name = string.IsNullOrWhiteSpace(raw.BuyerHint) ? "Alıcı (AI)" : raw.BuyerHint!,
                VatId = raw.BuyerVatIdHint,
            },
            Lines = lines,
        };
    }
}
