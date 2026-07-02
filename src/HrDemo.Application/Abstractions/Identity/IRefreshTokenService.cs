using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Abstractions.Identity;

public interface IRefreshTokenService
{
    Task<string> CreateRefreshTokenAsync(int userId, string jwtId, CancellationToken cancellationToken = default);
    Task<ResponseResult<LoginResponseDto>> RotateTokenAsync(string plainRefreshToken, CancellationToken cancellationToken = default);
    Task<ResponseResult> RevokeTokenAsync(string plainRefreshToken, CancellationToken cancellationToken = default);
}
