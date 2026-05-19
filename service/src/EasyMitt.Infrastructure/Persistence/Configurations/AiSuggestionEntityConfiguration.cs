using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyMitt.Infrastructure.Persistence.Configurations;

public sealed class AiSuggestionEntityConfiguration : IEntityTypeConfiguration<AiSuggestionEntity>
{
    public void Configure(EntityTypeBuilder<AiSuggestionEntity> builder)
    {
        builder.ToTable("ai_suggestions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SuggestionType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TargetType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(32);
        builder.Property(x => x.DecidedByUserEmail).HasMaxLength(256);
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.SuggestionType, x.Status });
        builder.HasIndex(x => new { x.CompanyId, x.TargetType, x.TargetId });
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
