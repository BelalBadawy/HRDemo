using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Abstractions.Identity;

public interface IRefreshTokenService
{
    Task<string> CreateRefreshTokenAsync(int userId, string jwtId, string ipAddress, CancellationToken cancellationToken = default);
    Task<ResponseResult<LoginResponseDto>> RotateTokenAsync(string plainRefreshToken, string ipAddress, CancellationToken cancellationToken = default);
    Task<ResponseResult> RevokeTokenAsync(string plainRefreshToken, string ipAddress, CancellationToken cancellationToken = default);
}
