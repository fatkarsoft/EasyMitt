using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class ProductEntityConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Sku).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(32);
        builder.Property(x => x.NetPrice).HasPrecision(18, 2);
        builder.Property(x => x.VatRatePercent).HasPrecision(5, 2);
        builder.Property(x => x.CurrentStock).HasPrecision(18, 3);
        builder.Property(x => x.MinimumStock).HasPrecision(18, 3);
        builder.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.Name });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
