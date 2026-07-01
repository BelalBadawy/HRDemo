using Mediator;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Common.Behaviors;

namespace HrDemo.Application.Features.Authentication.Commands.AssignRole;

public sealed record AssignRoleCommand(int UserId, string Role) : IRequest<ResponseResult>, IAuthorizeRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => new[] { "roles.assign" };
}
