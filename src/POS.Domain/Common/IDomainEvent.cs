namespace POS.Domain.Common;

/// <summary>
/// Marker interface for all domain events in the system.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();
    DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
