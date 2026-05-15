using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementEntityConfiguration : IEntityTypeConfiguration<InventoryMovementEntity>
{
    public void Configure(EntityTypeBuilder<InventoryMovementEntity> builder)
    {
        builder.ToTable("inventory_movements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(32);
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 3);
        builder.Property(x => x.Reason).HasMaxLength(512);
        builder.HasIndex(x => new { x.CompanyId, x.ProductId, x.CreatedAtUtc });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
