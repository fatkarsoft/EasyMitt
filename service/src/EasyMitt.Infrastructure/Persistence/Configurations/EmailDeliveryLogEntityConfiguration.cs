using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class EmailDeliveryLogEntityConfiguration : IEntityTypeConfiguration<EmailDeliveryLogEntity>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryLogEntity> builder)
    {
        builder.ToTable("email_delivery_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ToEmail).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(512);
        builder.Property(x => x.AttachmentType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(32);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2048);
        builder.Property(x => x.SenderUserId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.SenderUserEmail).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => new { x.CompanyId, x.DocumentType, x.DocumentId });
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
