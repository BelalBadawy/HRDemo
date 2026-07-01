using Mediator;
using HrDemo.Application.Abstractions.Events;
using HrDemo.Domain.Interfaces;

namespace HrDemo.Infrastructure.Services;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        if (domainEvents == null)
        {
            return;
        }

        foreach (var domainEvent in domainEvents)
        {
            var notification = new DomainEventNotification(domainEvent);
            await _mediator.Publish(notification, cancellationToken);
        }
    }
}
