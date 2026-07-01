using HrDemo.Application.Abstractions.DateTime;

namespace HrDemo.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    private readonly TimeProvider _timeProvider;

    public SystemClock() : this(TimeProvider.System)
    {
    }

    public SystemClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
}
