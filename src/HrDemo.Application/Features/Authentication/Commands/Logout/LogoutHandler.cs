using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;

namespace HrDemo.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, ResponseResult>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public LogoutHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async ValueTask<ResponseResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        return await _refreshTokenService.RevokeTokenAsync(request.RefreshToken, request.IpAddress, cancellationToken);
    }
}
