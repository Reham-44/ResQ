using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Domain.Entities;

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

        builder.Property(e => e.RequiredTeamType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.Name).IsUnique();
    }
}
