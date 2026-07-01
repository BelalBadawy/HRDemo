using HrDemo.Domain.Interfaces;

namespace HrDemo.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
