namespace EasyMitt.Domain.Quotes;

public static class QuoteStatus
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
    public const string Converted = "Converted";

    public static bool IsKnown(string status) =>
        status is Draft or Sent or Accepted or Declined or Converted;
}
