using ResQ.Domain.Enums;

namespace ResQ.Domain.Exceptions;

public class InvalidStatusTransitionException : DomainException
{
    public EmergencyStatus From { get; }
    public EmergencyStatus To { get; }

    public InvalidStatusTransitionException(EmergencyStatus from, EmergencyStatus to)
        : base($"Cannot transition emergency from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }
}
