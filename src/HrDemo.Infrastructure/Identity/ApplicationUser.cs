using Microsoft.AspNetCore.Identity;

namespace HrDemo.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<int>
{
    public RefreshToken? RefreshToken { get; set; }
}
