using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Features.Authentication.Commands.Login;

public sealed class LoginHandler : IRequestHandler<LoginCommand, ResponseResult<LoginResponseDto>>
{
    private readonly IUserManager _userManager;

    public LoginHandler(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async ValueTask<ResponseResult<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return await _userManager.LoginAsync(request.UserNameOrEmail, request.Password, cancellationToken);
    }
}
