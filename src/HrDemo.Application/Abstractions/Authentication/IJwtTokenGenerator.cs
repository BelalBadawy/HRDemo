using System.Security.Claims;

namespace HrDemo.Application.Abstractions.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateToken(int userId, string userName, IEnumerable<Claim> claims);
}
