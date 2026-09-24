using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;

namespace ResQ.Infrastructure.Persistence.Configurations;

public class EmergencyTypeConfiguration : IEntityTypeConfiguration<EmergencyType>
{
    public void Configure(EntityTypeBuilder<EmergencyType> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.RequiredTeamType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TeamType.Medical)
            .HasSentinel((TeamType)0);

        builder.HasIndex(e => e.Name).IsUnique();
    }
}
