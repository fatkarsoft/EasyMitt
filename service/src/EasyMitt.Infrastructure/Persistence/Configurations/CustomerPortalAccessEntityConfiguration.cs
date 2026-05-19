using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class CustomerPortalAccessEntityConfiguration : IEntityTypeConfiguration<CustomerPortalAccessEntity>
{
    public void Configure(EntityTypeBuilder<CustomerPortalAccessEntity> builder)
    {
        builder.ToTable("customer_portal_access");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(128);
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.TokenPrefix).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(32);
        builder.Property(x => x.CreatedByUserEmail).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.CustomerId, x.Status });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
