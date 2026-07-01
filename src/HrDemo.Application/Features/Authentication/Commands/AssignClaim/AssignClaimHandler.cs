using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.AssignClaim;

public sealed class AssignClaimHandler : IRequestHandler<AssignClaimCommand, ResponseResult>
{
    private readonly IUserManager _userManager;

    public AssignClaimHandler(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async ValueTask<ResponseResult> Handle(AssignClaimCommand request, CancellationToken cancellationToken)
    {
        return await _userManager.AddClaimAsync(request.UserId, request.ClaimType, request.ClaimValue, cancellationToken);
    }
}
