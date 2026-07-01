using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.AssignRole;

public sealed class AssignRoleHandler : IRequestHandler<AssignRoleCommand, ResponseResult>
{
    private readonly IUserManager _userManager;

    public AssignRoleHandler(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async ValueTask<ResponseResult> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        return await _userManager.AddToRoleAsync(request.UserId, request.Role, cancellationToken);
    }
}
