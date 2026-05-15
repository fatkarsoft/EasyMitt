using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class BankTransactionEntityConfiguration : IEntityTypeConfiguration<BankTransactionEntity>
{
    public void Configure(EntityTypeBuilder<BankTransactionEntity> builder)
    {
        builder.ToTable("bank_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(512);
        builder.Property(x => x.CounterpartyName).HasMaxLength(256);
        builder.Property(x => x.CounterpartyIban).HasMaxLength(34);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Direction).IsRequired().HasMaxLength(16);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(24);
        builder.Property(x => x.Source).HasMaxLength(64);
        builder.HasIndex(x => new { x.CompanyId, x.BookingDate });
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Allocations).WithOne(x => x.BankTransaction).HasForeignKey(x => x.BankTransactionId).OnDelete(DeleteBehavior.Cascade);
    }
}
