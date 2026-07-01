namespace HrDemo.Application.Abstractions.DateTime;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
