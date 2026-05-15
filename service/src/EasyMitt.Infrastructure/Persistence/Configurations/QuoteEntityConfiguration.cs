using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class QuoteEntityConfiguration : IEntityTypeConfiguration<QuoteEntity>
{
    public void Configure(EntityTypeBuilder<QuoteEntity> builder)
    {
        builder.ToTable("quotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LineProductIdsJson).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.QuoteNumber).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(24);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.QuoteNumber }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.Status });
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
    }
}
