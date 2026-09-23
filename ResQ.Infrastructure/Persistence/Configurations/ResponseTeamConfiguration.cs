using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Domain.Entities;

namespace ResQ.Infrastructure.Persistence.Configurations;

public class ResponseTeamConfiguration : IEntityTypeConfiguration<ResponseTeam>
{
    public void Configure(EntityTypeBuilder<ResponseTeam> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.TeamType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Latitude)
            .HasColumnType("decimal(10,7)");

        builder.Property(t => t.Longitude)
            .HasColumnType("decimal(10,7)");

        // Optimistic concurrency token
        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.TeamType);
    }
}
