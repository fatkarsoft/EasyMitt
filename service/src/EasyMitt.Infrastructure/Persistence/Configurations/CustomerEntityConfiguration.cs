using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(32);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.CompanyName).HasMaxLength(256);
        builder.Property(x => x.FirstName).HasMaxLength(128);
        builder.Property(x => x.LastName).HasMaxLength(128);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);
        builder.Property(x => x.Street).HasMaxLength(256);
        builder.Property(x => x.PostalCode).HasMaxLength(32);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
        builder.Property(x => x.VatId).HasMaxLength(32);
        builder.Property(x => x.TaxNumber).HasMaxLength(64);
        builder.Property(x => x.LeitwegId).HasMaxLength(64);
        builder.Property(x => x.DatevDebitorAccount).HasMaxLength(16);
        builder.Property(x => x.Notes).HasMaxLength(2048);
        builder.HasIndex(x => new { x.CompanyId, x.DisplayName });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
