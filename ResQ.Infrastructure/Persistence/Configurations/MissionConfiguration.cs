using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Domain.Entities;

namespace ResQ.Infrastructure.Persistence.Configurations;

public class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(m => new { m.EmergencyId, m.ResponseTeamId });
        builder.HasIndex(m => m.Status);

        builder.HasOne(m => m.ResponseTeam)
            .WithMany(t => t.Missions)
            .HasForeignKey(m => m.ResponseTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
