using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class DatevExportLogEntityConfiguration : IEntityTypeConfiguration<DatevExportLogEntity>
{
    public void Configure(EntityTypeBuilder<DatevExportLogEntity> builder)
    {
        builder.ToTable("datev_export_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExportType).IsRequired().HasMaxLength(32);
        builder.Property(x => x.StatusFilter).HasMaxLength(32);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.Sha256Hex).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ArchiveObjectKey).HasMaxLength(512);
        builder.Property(x => x.UserEmail).IsRequired().HasMaxLength(256);
        builder.Property(x => x.UserDisplayName).IsRequired().HasMaxLength(256);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalTaxAmount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.ExportType, x.StatusFilter, x.PeriodFrom, x.PeriodTo });
        builder.HasIndex(x => x.Sha256Hex);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
