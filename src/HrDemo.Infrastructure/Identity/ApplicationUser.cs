using Microsoft.AspNetCore.Identity;

namespace HrDemo.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<int>
{
    // Note: ApplicationUser uses CreatedDate instead of CreatedAt because it sits in the 
    // Identity/Infrastructure layer rather than the domain BaseEntity/BaseAuditableEntity hierarchy.
    public DateTimeOffset CreatedDate { get; set; }
    
    public bool IsActive { get; set; } = true;

    public RefreshToken? RefreshToken { get; set; }
}
