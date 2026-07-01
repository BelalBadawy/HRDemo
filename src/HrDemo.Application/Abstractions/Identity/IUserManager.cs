using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;

namespace HrDemo.Application.Abstractions.Identity;

public interface IUserManager
{
    Task<ResponseResult<int>> CreateUserAsync(string userName, string email, string password, CancellationToken cancellationToken = default);
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsUserNameUniqueAsync(string userName, CancellationToken cancellationToken = default);
    Task<ResponseResult> AddToRoleAsync(int userId, string role, CancellationToken cancellationToken = default);
    Task<ResponseResult> AddClaimAsync(int userId, string claimType, string claimValue, CancellationToken cancellationToken = default);
    Task<ResponseResult<LoginResponseDto>> LoginAsync(string userNameOrEmail, string password, string ipAddress, CancellationToken cancellationToken = default);
}
