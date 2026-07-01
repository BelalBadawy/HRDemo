using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using HrDemo.Application.Abstractions.Authentication;
using HrDemo.Application.Abstractions.DateTime;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;
using HrDemo.Infrastructure.Persistence;

namespace HrDemo.Infrastructure.Identity;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IClock _clock;
    private readonly JwtOptions _jwtOptions;
    private readonly UserManager<ApplicationUser> _userManager;

    public RefreshTokenService(
        ApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IClock clock,
        IOptions<JwtOptions> jwtOptions,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _clock = clock;
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager;
    }

    public async Task<string> CreateRefreshTokenAsync(int userId, string jwtId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(plainToken);

        var existingToken = await _context.RefreshTokens.FindAsync(new object[] { userId }, cancellationToken);
        if (existingToken != null)
        {
            existingToken.TokenHash = hash;
            existingToken.JwtId = jwtId;
            existingToken.ExpiryTime = _clock.UtcNow.Add(_jwtOptions.RefreshTokenLifetime);
            existingToken.CreatedAt = _clock.UtcNow;
            existingToken.CreatedByIp = ipAddress;
            existingToken.RevokedAt = null;
            existingToken.RevokedByIp = null;
        }
        else
        {
            var token = new RefreshToken
            {
                UserId = userId,
                TokenHash = hash,
                JwtId = jwtId,
                ExpiryTime = _clock.UtcNow.Add(_jwtOptions.RefreshTokenLifetime),
                CreatedAt = _clock.UtcNow,
                CreatedByIp = ipAddress
            };
            _context.RefreshTokens.Add(token);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return plainToken;
    }

    public async Task<ResponseResult<LoginResponseDto>> RotateTokenAsync(string plainRefreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(plainRefreshToken);

        var tokenRecord = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (tokenRecord == null)
        {
            return ResponseResult<LoginResponseDto>.FailureResult(ResultStatus.Unauthorized, "Invalid refresh token.", 401);
        }

        if (tokenRecord.ExpiryTime < _clock.UtcNow)
        {
            return ResponseResult<LoginResponseDto>.FailureResult(ResultStatus.Unauthorized, "Refresh token has expired.", 401);
        }

        if (tokenRecord.RevokedAt != null)
        {
            return ResponseResult<LoginResponseDto>.FailureResult(ResultStatus.Unauthorized, "Refresh token has been revoked.", 401);
        }

        var user = tokenRecord.User;
        var claims = await _userManager.GetClaimsAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        var allClaims = new List<Claim>(claims);
        foreach (var role in roles)
        {
            allClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var newJwtId = Guid.NewGuid().ToString();
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.UserName!, allClaims);
        
        var newPlainRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newHash = HashToken(newPlainRefreshToken);

        // Update token record
        tokenRecord.TokenHash = newHash;
        tokenRecord.JwtId = newJwtId;
        tokenRecord.ExpiryTime = _clock.UtcNow.Add(_jwtOptions.RefreshTokenLifetime);
        tokenRecord.CreatedAt = _clock.UtcNow;
        tokenRecord.CreatedByIp = ipAddress;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResponseResult<LoginResponseDto>.FailureResult(
                ResultStatus.Conflict,
                "A concurrency conflict occurred. The session may have already been updated.",
                409
            );
        }

        return ResponseResult<LoginResponseDto>.SuccessResult(new LoginResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newPlainRefreshToken
        }, "Token refreshed successfully.");
    }

    public async Task<ResponseResult> RevokeTokenAsync(string plainRefreshToken, string ipAddress, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(plainRefreshToken);

        var tokenRecord = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);

        if (tokenRecord == null)
        {
            return ResponseResult.FailureResult(ResultStatus.NotFound, "Refresh token not found.", 404);
        }

        if (tokenRecord.RevokedAt != null)
        {
            return ResponseResult.SuccessResult("Token was already revoked.");
        }

        // Under a Single Refresh Token Policy, we delete the row upon logout/revocation.
        _context.RefreshTokens.Remove(tokenRecord);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResponseResult.FailureResult(
                ResultStatus.Conflict,
                "A concurrency conflict occurred while revoking the token.",
                409
            );
        }

        return ResponseResult.SuccessResult("Token revoked successfully.");
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
