namespace HrDemo.Application.Abstractions.Identity;

public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
