using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class DispatchLogEntityConfiguration : IEntityTypeConfiguration<DispatchLogEntity>
{
    public void Configure(EntityTypeBuilder<DispatchLogEntity> builder)
    {
        builder.ToTable("dispatch_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Backend).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(64);
        builder.Property(x => x.PartnerId).HasMaxLength(256);
        builder.Property(x => x.ResponseJson);
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
