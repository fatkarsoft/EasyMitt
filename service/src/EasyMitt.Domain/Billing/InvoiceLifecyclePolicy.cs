namespace EasyMitt.Domain.Billing;

public static class InvoiceLifecyclePolicy
{
    public static bool CanTransition(string currentStatus, string nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (InvoiceLifecycleStatus.Draft, InvoiceLifecycleStatus.Issued) => true,
            (InvoiceLifecycleStatus.Issued, InvoiceLifecycleStatus.Sent) => true,
            (InvoiceLifecycleStatus.Issued, InvoiceLifecycleStatus.PartiallyPaid) => true,
            (InvoiceLifecycleStatus.Issued, InvoiceLifecycleStatus.Paid) => true,
            (InvoiceLifecycleStatus.Issued, InvoiceLifecycleStatus.Overdue) => true,
            (InvoiceLifecycleStatus.Issued, InvoiceLifecycleStatus.Cancelled) => true,
            (InvoiceLifecycleStatus.Sent, InvoiceLifecycleStatus.PartiallyPaid) => true,
            (InvoiceLifecycleStatus.Sent, InvoiceLifecycleStatus.Paid) => true,
            (InvoiceLifecycleStatus.Sent, InvoiceLifecycleStatus.Overdue) => true,
            (InvoiceLifecycleStatus.Sent, InvoiceLifecycleStatus.Cancelled) => true,
            (InvoiceLifecycleStatus.PartiallyPaid, InvoiceLifecycleStatus.Paid) => true,
            (InvoiceLifecycleStatus.PartiallyPaid, InvoiceLifecycleStatus.Overdue) => true,
            (InvoiceLifecycleStatus.PartiallyPaid, InvoiceLifecycleStatus.Cancelled) => true,
            (InvoiceLifecycleStatus.Overdue, InvoiceLifecycleStatus.PartiallyPaid) => true,
            (InvoiceLifecycleStatus.Overdue, InvoiceLifecycleStatus.Paid) => true,
            (InvoiceLifecycleStatus.Overdue, InvoiceLifecycleStatus.Cancelled) => true,
            _ => false,
        };
}
