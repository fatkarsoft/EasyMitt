using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class ExpenseEntityConfiguration : IEntityTypeConfiguration<ExpenseEntity>
{
    public void Configure(EntityTypeBuilder<ExpenseEntity> builder)
    {
        builder.ToTable("expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VendorName).IsRequired().HasMaxLength(240);
        builder.Property(x => x.DocumentNumber).HasMaxLength(120);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(80);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(x => x.DatevCreditorAccount).HasMaxLength(16);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(24);
        builder.Property(x => x.Notes).HasMaxLength(1024);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
