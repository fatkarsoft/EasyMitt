using EasyMitt.Application.Dtos.En16931;
using s2industries.ZUGFeRD;

namespace EasyMitt.Infrastructure.ElectronicInvoicing;

internal static class InvoiceDescriptorMapper
{
    internal const string DefaultBusinessProcess = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";

    public static InvoiceDescriptor ToDescriptor(InvoiceDocumentDto d)
    {
        var issue = d.Core.IssueDate.ToDateTime(TimeOnly.MinValue);
        var currency = ParseCurrency(d.Core.CurrencyCode);
        var desc = InvoiceDescriptor.CreateInvoice(d.Core.InvoiceNumber, issue, currency);
        desc.BusinessProcess = DefaultBusinessProcess;
        desc.Name = "Rechnung";
        desc.ReferenceOrderNo = d.Core.BuyerReference.Trim();

        desc.SetSeller(
            name: d.Seller.Name,
            postcode: "00000",
            city: "—",
            street: "—",
            country: CountryCodes.DE,
            id: string.Empty,
            globalID: null,
            legalOrganization: null);

        if (!string.IsNullOrWhiteSpace(d.Seller.VatId))
        {
            var sellerVatId = d.Seller.VatId.Trim();
            desc.SetSellerElectronicAddress(sellerVatId, ElectronicAddressSchemeIdentifiers.GermanyVatNumber);
            desc.AddSellerTaxRegistration(sellerVatId, TaxRegistrationSchemeID.VA);
        }

        desc.SetBuyer(
            name: d.Buyer.Name,
            postcode: "00000",
            city: "—",
            street: "—",
            country: CountryCodes.DE,
            id: string.Empty);

        if (!string.IsNullOrWhiteSpace(d.Seller.PaymentIban))
        {
            desc.SetPaymentMeans(PaymentMeansTypeCodes.SEPACreditTransfer, "SEPA-Überweisung");
            desc.AddCreditorFinancialAccount(iban: d.Seller.PaymentIban.Trim().Replace(" ", "", StringComparison.Ordinal), bic: null, name: d.Seller.Name);
        }

        if (!string.IsNullOrWhiteSpace(d.Core.BuyerReference))
        {
            desc.SetBuyerElectronicAddress(d.Core.BuyerReference.Trim(), ElectronicAddressSchemeIdentifiers.LeitwegId);
        }

        if (!string.IsNullOrWhiteSpace(d.Buyer.VatId))
        {
            desc.AddBuyerTaxRegistration(d.Buyer.VatId.Trim(), TaxRegistrationSchemeID.VA);
        }

        foreach (var line in d.Lines)
        {
            var qty = line.Quantity <= 0 ? 1m : line.Quantity;
            var netUnit = qty == 0 ? 0m : decimal.Round(line.NetAmount / qty, 4, MidpointRounding.AwayFromZero);
            desc.AddTradeLineItem(
                name: line.ItemName,
                unitCode: QuantityCodes.H87,
                sellerAssignedID: null,
                id: null,
                grossUnitPrice: netUnit,
                netUnitPrice: netUnit,
                billedQuantity: qty,
                taxType: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.S,
                taxPercent: line.VatRatePercent);
        }

        var (lineNet, taxByRate) = SummarizeLines(d.Lines);
        foreach (var kv in taxByRate.OrderBy(x => x.Key))
        {
            var basis = kv.Value.Basis;
            var pct = kv.Key;
            var taxAmt = decimal.Round(basis * pct / 100m, 2, MidpointRounding.AwayFromZero);
            desc.AddApplicableTradeTax(
                basisAmount: basis,
                percent: pct,
                taxAmount: taxAmt,
                typeCode: TaxTypes.VAT,
                categoryCode: TaxCategoryCodes.S);
        }

        var taxTotal = taxByRate.Values.Sum(x => decimal.Round(x.Basis * x.Rate / 100m, 2, MidpointRounding.AwayFromZero));
        var declaredTaxTotal = d.Core.TaxAmount;
        var grand = d.Core.InvoiceTotalVatIncluded > 0 ? d.Core.InvoiceTotalVatIncluded : lineNet + taxTotal;
        var basisForTotals = d.Core.InvoiceTotalVatIncluded > 0 && declaredTaxTotal >= 0
            ? grand - declaredTaxTotal
            : lineNet;

        desc.SetTotals(
            lineTotalAmount: lineNet,
            taxBasisAmount: basisForTotals,
            taxTotalAmount: declaredTaxTotal > 0 ? declaredTaxTotal : taxTotal,
            grandTotalAmount: grand,
            duePayableAmount: grand);

        return desc;
    }

    private static CurrencyCodes ParseCurrency(string code)
    {
        if (Enum.TryParse<CurrencyCodes>(code.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return CurrencyCodes.EUR;
    }

    private static (decimal lineNet, Dictionary<decimal, (decimal Basis, decimal Rate)> taxByRate) SummarizeLines(
        IReadOnlyList<InvoiceLineDto> lines)
    {
        var taxByRate = new Dictionary<decimal, decimal>();
        decimal lineNet = 0;
        foreach (var line in lines)
        {
            lineNet += line.NetAmount;
            if (!taxByRate.ContainsKey(line.VatRatePercent))
            {
                taxByRate[line.VatRatePercent] = 0;
            }

            taxByRate[line.VatRatePercent] += line.NetAmount;
        }

        var dict = taxByRate.ToDictionary(
            kv => kv.Key,
            kv => (Basis: kv.Value, Rate: kv.Key));
        return (lineNet, dict);
    }
}
