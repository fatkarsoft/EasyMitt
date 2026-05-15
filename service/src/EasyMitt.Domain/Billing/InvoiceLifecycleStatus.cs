namespace EasyMitt.Domain.Billing;

public static class InvoiceLifecycleStatus
{
    public const string Draft = "Draft";
    public const string Issued = "Issued";
    public const string Sent = "Sent";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Overdue = "Overdue";
    public const string Cancelled = "Cancelled";

    public static bool IsKnown(string status) =>
        status is Draft or Issued or Sent or PartiallyPaid or Paid or Overdue or Cancelled;
}
