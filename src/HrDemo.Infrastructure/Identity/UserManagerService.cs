using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Abstractions.Authentication;
using HrDemo.Application.Features.Authentication.Dtos;
using HrDemo.Application.Common.Results;

namespace HrDemo.Infrastructure.Identity;

public sealed class UserManagerService : IUserManager
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserManagerService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ResponseResult<int>> CreateUserAsync(string userName, string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return ResponseResult<int>.FailureResult(
                ResultStatus.ValidationError,
                "User creation failed.",
                400,
                errors
            );
        }

        return ResponseResult<int>.SuccessResult(user.Id, "User created successfully.", 201);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null;
    }

    public async Task<bool> IsUserNameUniqueAsync(string userName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(userName);
        return user == null;
    }

    public async Task<ResponseResult> AddToRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ResponseResult.FailureResult(ResultStatus.NotFound, "User not found.", 404);
        }

        // Ensure role exists
        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            var newRole = new ApplicationRole { Name = role };
            await _roleManager.CreateAsync(newRole);
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return ResponseResult.FailureResult(ResultStatus.Error, "Failed to assign role.");
        }

        return ResponseResult.SuccessResult("Role assigned successfully.");
    }

    public async Task<ResponseResult> AddClaimAsync(int userId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return ResponseResult.FailureResult(ResultStatus.NotFound, "User not found.", 404);
        }

        var result = await _userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            return ResponseResult.FailureResult(ResultStatus.Error, "Failed to assign claim.");
        }

        return ResponseResult.SuccessResult("Claim assigned successfully.");
    }

    public async Task<ResponseResult<LoginResponseDto>> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByNameAsync(userNameOrEmail);
        if (user == null && userNameOrEmail.Contains('@', StringComparison.Ordinal))
        {
            user = await _userManager.FindByEmailAsync(userNameOrEmail);
        }

        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return ResponseResult<LoginResponseDto>.FailureResult(
                ResultStatus.Unauthorized,
                "Invalid username or password.",
                401
            );
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        var allClaims = new List<Claim>(claims);
        foreach (var role in roles)
        {
            allClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.UserName!, allClaims);
        var refreshToken = Guid.NewGuid().ToString("N");

        return ResponseResult<LoginResponseDto>.SuccessResult(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        }, "Login successful.");
    }
}
