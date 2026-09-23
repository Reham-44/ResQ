using ResQ.Domain.Enums;
using ResQ.Domain.Exceptions;

namespace ResQ.Domain.Entities;

public class ResponseTeam
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TeamType TeamType { get; private set; }
    public TeamStatus Status { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    // Concurrency token – configured in EF Core
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Mission> Missions => _missions.AsReadOnly();
    private readonly List<Mission> _missions = [];

    private ResponseTeam() { }

    public ResponseTeam(string name, TeamType teamType, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name cannot be empty.", nameof(name));

        Name = name;
        TeamType = teamType;
        Status = TeamStatus.Available;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void MarkBusy()
    {
        if (Status != TeamStatus.Available)
            throw new DomainException($"Team '{Name}' cannot be assigned because its status is '{Status}'.");

        Status = TeamStatus.Busy;
    }

    public void MarkAvailable()
    {
        if (Status == TeamStatus.Offline)
            throw new DomainException($"Team '{Name}' is offline.");
        Status = TeamStatus.Available;
    }

    public void UpdateStatus(TeamStatus newStatus) => Status = newStatus;

    public void UpdateLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
