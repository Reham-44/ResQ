using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;

namespace ResQ.Infrastructure.Persistence.Configurations;

public class EmergencyConfiguration : IEntityTypeConfiguration<Emergency>
{
    public void Configure(EntityTypeBuilder<Emergency> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property(e => e.TrackingNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(e => e.TrackingNumber).IsUnique();

        builder.Property(e => e.CitizenId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // Coordinate precision: decimal(10,7) gives ~1 cm accuracy
        builder.Property(e => e.Latitude)
            .HasColumnType("decimal(10,7)");

        builder.Property(e => e.Longitude)
            .HasColumnType("decimal(10,7)");

        builder.Property(e => e.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.ResponseDeadline)
            .IsRequired();

        // Useful indexes
        builder.HasIndex(e => e.CitizenId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasOne(e => e.EmergencyType)
            .WithMany()
            .HasForeignKey(e => e.EmergencyTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Missions)
            .WithOne(m => m.Emergency)
            .HasForeignKey(m => m.EmergencyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.History)
            .WithOne(h => h.Emergency)
            .HasForeignKey(h => h.EmergencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
