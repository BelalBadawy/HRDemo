using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.Register;

public sealed class RegisterHandler : IRequestHandler<RegisterCommand, ResponseResult<int>>
{
    private readonly IUserManager _userManager;

    public RegisterHandler(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async ValueTask<ResponseResult<int>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return await _userManager.CreateUserAsync(request.UserName, request.Email, request.Password, cancellationToken);
    }
}
