namespace EasyMitt.Application.Localization;

public static class ValidationErrorCodes
{
    public const string Required = "validation.required";
    public const string MaxLength = "validation.max_length";
    public const string ExactLength = "validation.exact_length";
    public const string GreaterThanZero = "validation.greater_than_zero";
    public const string GreaterThanOrEqualZero = "validation.greater_than_or_equal_zero";
    public const string IbanRequired = "validation.iban_required";
    public const string IbanFormat = "validation.iban_format";
    public const string SellerVatRequired = "validation.seller_vat_required";
    public const string LinesRequired = "validation.lines_required";
    public const string GermanVatRate = "validation.german_vat_rate";
}
