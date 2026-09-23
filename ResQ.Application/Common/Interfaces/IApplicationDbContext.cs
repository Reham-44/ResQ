using Microsoft.EntityFrameworkCore;
using ResQ.Domain.Entities;

namespace ResQ.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Emergency> Emergencies { get; }
    DbSet<EmergencyType> EmergencyTypes { get; }
    DbSet<ResponseTeam> ResponseTeams { get; }
    DbSet<Mission> Missions { get; }
    DbSet<EmergencyHistory> EmergencyHistories { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
