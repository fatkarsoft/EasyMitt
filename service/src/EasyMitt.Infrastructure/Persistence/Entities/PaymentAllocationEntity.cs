namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class PaymentAllocationEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankTransactionId { get; set; }
    public Guid InvoiceDraftId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
    public BankTransactionEntity? BankTransaction { get; set; }
    public InvoiceDraftEntity? InvoiceDraft { get; set; }
}
