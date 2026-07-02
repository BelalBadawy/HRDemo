namespace HrDemo.Application.Abstractions.Identity;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    bool HasPermission(string permission);
}
