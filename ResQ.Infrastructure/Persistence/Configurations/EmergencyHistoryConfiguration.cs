using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Domain.Entities;

namespace ResQ.Infrastructure.Persistence.Configurations;

public class EmergencyHistoryConfiguration : IEntityTypeConfiguration<EmergencyHistory>
{
    public void Configure(EntityTypeBuilder<EmergencyHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.OldStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.PerformedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        builder.HasIndex(h => h.EmergencyId);
        builder.HasIndex(h => h.CreatedAt);
    }
}
