using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ResQ.Domain.Entities;
using ResQ.Infrastructure.Identity;

using ResQ.Application.Common.Interfaces;

namespace ResQ.Infrastructure.Persistence;

public class ResQDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ResQDbContext(DbContextOptions<ResQDbContext> options) : base(options) { }

    public DbSet<Emergency> Emergencies => Set<Emergency>();
    public DbSet<EmergencyType> EmergencyTypes => Set<EmergencyType>();
    public DbSet<ResponseTeam> ResponseTeams => Set<ResponseTeam>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<EmergencyHistory> EmergencyHistories => Set<EmergencyHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResQDbContext).Assembly);
    }
}
