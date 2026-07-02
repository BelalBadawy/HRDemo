using Mediator;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Features.Authentication.Commands.Refresh;

public sealed class RefreshHandler : IRequestHandler<RefreshCommand, ResponseResult<LoginResponseDto>>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async ValueTask<ResponseResult<LoginResponseDto>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        return await _refreshTokenService.RotateTokenAsync(request.RefreshToken, cancellationToken);
    }
}
