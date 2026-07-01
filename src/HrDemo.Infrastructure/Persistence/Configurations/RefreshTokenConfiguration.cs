using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HrDemo.Infrastructure.Identity;

namespace HrDemo.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.JwtId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(x => x.RevokedByIp)
            .HasMaxLength(50);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // 1-to-1 relationship with ApplicationUser
        builder.HasOne(x => x.User)
            .WithOne(u => u.RefreshToken)
            .HasForeignKey<RefreshToken>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
