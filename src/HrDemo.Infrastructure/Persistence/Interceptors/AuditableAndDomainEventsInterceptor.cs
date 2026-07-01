using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using HrDemo.Application.Abstractions.DateTime;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Abstractions.Events;
using HrDemo.Domain.Common;
using HrDemo.Domain.Interfaces;

namespace HrDemo.Infrastructure.Persistence.Interceptors;

public sealed class AuditableAndDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly List<IDomainEvent> _collectedEvents = new();

    public AuditableAndDomainEventsInterceptor(
        ICurrentUser currentUser,
        IClock clock,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _currentUser = currentUser;
        _clock = clock;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            UpdateAuditFields(eventData.Context);
            CollectDomainEvents(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_collectedEvents.Count > 0)
        {
            var eventsToDispatch = _collectedEvents.ToList();
            _collectedEvents.Clear();
            await _domainEventDispatcher.DispatchEventsAsync(eventsToDispatch, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _collectedEvents.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void UpdateAuditFields(DbContext context)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedBy = userId;
            }
        }
    }

    private void CollectDomainEvents(DbContext context)
    {
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entities)
        {
            _collectedEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }
    }
}
