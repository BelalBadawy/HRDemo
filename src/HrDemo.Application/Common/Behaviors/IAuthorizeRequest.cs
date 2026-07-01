namespace HrDemo.Application.Common.Behaviors;

public interface IAuthorizeRequest
{
    IReadOnlyCollection<string> RequiredPermissions { get; }
}
