using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class InvoiceDraftEntityConfiguration : IEntityTypeConfiguration<InvoiceDraftEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceDraftEntity> builder)
    {
        builder.ToTable("invoice_drafts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.CanonicalSha256Hex).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ArchiveObjectKey).HasMaxLength(512);
        builder.HasIndex(x => x.CanonicalSha256Hex);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
