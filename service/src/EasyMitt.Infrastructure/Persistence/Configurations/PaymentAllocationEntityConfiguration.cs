using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class PaymentAllocationEntityConfiguration : IEntityTypeConfiguration<PaymentAllocationEntity>
{
    public void Configure(EntityTypeBuilder<PaymentAllocationEntity> builder)
    {
        builder.ToTable("payment_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceDraftId });
        builder.HasIndex(x => new { x.CompanyId, x.BankTransactionId });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.InvoiceDraft).WithMany().HasForeignKey(x => x.InvoiceDraftId).OnDelete(DeleteBehavior.Cascade);
    }
}
