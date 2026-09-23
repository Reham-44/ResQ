using ResQ.Domain.Enums;

namespace ResQ.Domain.Entities;

public class EmergencyType
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TeamType RequiredTeamType { get; private set; }

    private EmergencyType() { }

    public EmergencyType(string name, TeamType requiredTeamType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Emergency type name cannot be empty.", nameof(name));

        Name = name;
        RequiredTeamType = requiredTeamType;
        Description = description;
    }

    public EmergencyType(string name, string? description = null)
        : this(name, TeamType.Medical, description)
    {
    }
}
