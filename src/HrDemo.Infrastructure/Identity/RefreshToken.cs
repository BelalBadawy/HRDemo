using System;

namespace HrDemo.Infrastructure.Identity;

public sealed class RefreshToken
{
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public string TokenHash { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTimeOffset ExpiryTime { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
