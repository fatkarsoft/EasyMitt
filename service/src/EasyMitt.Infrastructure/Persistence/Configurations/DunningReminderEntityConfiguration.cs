using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class DunningReminderEntityConfiguration : IEntityTypeConfiguration<DunningReminderEntity>
{
    public void Configure(EntityTypeBuilder<DunningReminderEntity> builder)
    {
        builder.ToTable("dunning_reminders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OpenAmount).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(2048);
        builder.Property(x => x.UserEmail).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => new { x.CompanyId, x.InvoiceDraftId, x.CreatedAtUtc });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.InvoiceDraft).WithMany().HasForeignKey(x => x.InvoiceDraftId).OnDelete(DeleteBehavior.Cascade);
    }
}
