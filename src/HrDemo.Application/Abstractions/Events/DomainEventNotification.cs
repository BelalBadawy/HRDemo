using Mediator;
using HrDemo.Domain.Interfaces;

namespace HrDemo.Application.Abstractions.Events;

public sealed record DomainEventNotification(IDomainEvent Event) : INotification;
