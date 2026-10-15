using MediatR;

namespace POS.Domain.Common;

/// <summary>
/// Marker interface for all domain events in the system.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();
    DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}

