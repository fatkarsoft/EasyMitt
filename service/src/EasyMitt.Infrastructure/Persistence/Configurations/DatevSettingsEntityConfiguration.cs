using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class DatevSettingsEntityConfiguration : IEntityTypeConfiguration<DatevSettingsEntity>
{
    public void Configure(EntityTypeBuilder<DatevSettingsEntity> builder)
    {
        builder.ToTable("datev_settings");
        builder.HasKey(x => x.CompanyId);
        builder.Property(x => x.ExportFormat).IsRequired().HasMaxLength(24).HasDefaultValue("BasicCsv");
        builder.Property(x => x.ChartOfAccounts).IsRequired().HasMaxLength(16);
        builder.Property(x => x.RevenueAccount).IsRequired().HasMaxLength(16);
        builder.Property(x => x.DefaultExpenseAccount).IsRequired().HasMaxLength(16);
        builder.Property(x => x.CustomerContraAccount).IsRequired().HasMaxLength(16);
        builder.Property(x => x.VendorContraAccount).IsRequired().HasMaxLength(16);
        builder.Property(x => x.ConsultantNumber).HasMaxLength(32);
        builder.Property(x => x.ClientNumber).HasMaxLength(32);
        builder.Property(x => x.ExpenseAccountMappingsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TaxKeyMappingsJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("[]");
        builder.HasOne(x => x.Company)
            .WithOne()
            .HasForeignKey<DatevSettingsEntity>(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
