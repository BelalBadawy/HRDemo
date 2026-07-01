using Mediator;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Common.Behaviors;

namespace HrDemo.Application.Features.Authentication.Commands.AssignClaim;

public sealed record AssignClaimCommand(int UserId, string ClaimType, string ClaimValue) : IRequest<ResponseResult>, IAuthorizeRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => new[] { "claims.assign" };
}
