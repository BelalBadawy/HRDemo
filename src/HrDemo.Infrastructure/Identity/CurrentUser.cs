using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using HrDemo.Application.Abstractions.Identity;

namespace HrDemo.Infrastructure.Identity;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return false;
        }

        return user.Claims.Any(c =>
            c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase) && c.Value.Equals(permission, StringComparison.Ordinal)
        );
    }
}
