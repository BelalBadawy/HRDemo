using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HrDemo.Infrastructure.Identity;

public sealed class PermissionSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PermissionSeeder> _logger;

    private static readonly string[] Roles = { "Admin", "User" };
    private static readonly string[] Permissions = 
    {
        "roles.create",
        "roles.view",
        "roles.update",
        "roles.delete",
        "roles.list",
        "roles.assign",
        "claims.assign"
    };

    public PermissionSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger<PermissionSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding process...");

        // 1. Seed Roles
        foreach (var roleName in Roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogInformation("Seeding role '{RoleName}'...", roleName);
                var role = new ApplicationRole { Name = roleName };
                try
                {
                    var result = await _roleManager.CreateAsync(role);
                    if (!result.Succeeded && result.Errors.All(e => e.Code != "DuplicateRoleName"))
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
                    }
                }
                catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        throw;
                    }
                    _logger.LogInformation("Role '{RoleName}' was seeded concurrently.", roleName);
                }
            }
        }

        // 2. Read Default Admin Configuration
        var adminSection = _configuration.GetSection("DefaultAdmin");
        var userName = adminSection["UserName"];
        var email = adminSection["Email"];
        var password = adminSection["Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Default admin credentials are not configured or incomplete in DefaultAdmin section. Skipping admin user seeding.");
            return;
        }

        // 3. Seed Default Admin User
        var adminUser = await _userManager.FindByEmailAsync(email);
        if (adminUser == null)
        {
            adminUser = await _userManager.FindByNameAsync(userName);
        }

        if (adminUser == null)
        {
            _logger.LogInformation("Seeding default admin user '{UserName}' ({Email})...", userName, email);
            adminUser = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };

            try
            {
                var result = await _userManager.CreateAsync(adminUser, password);
                if (!result.Succeeded && result.Errors.All(e => e.Code != "DuplicateUserName" && e.Code != "DuplicateEmail"))
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create default admin user: {errors}");
                }
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                adminUser = await _userManager.FindByEmailAsync(email);
                if (adminUser == null)
                {
                    adminUser = await _userManager.FindByNameAsync(userName);
                }

                if (adminUser == null)
                {
                    throw;
                }
                _logger.LogInformation("Default admin user was seeded concurrently.");
            }
        }

        // 4. Assign Admin Role to User
        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            _logger.LogInformation("Assigning 'Admin' role to user '{UserName}'...", userName);
            try
            {
                var result = await _userManager.AddToRoleAsync(adminUser, "Admin");
                if (!result.Succeeded && result.Errors.All(e => e.Code != "UserAlreadyInRole"))
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign 'Admin' role to user '{userName}': {errors}");
                }
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    throw;
                }
            }
        }

        // 5. Assign Permissions to User
        var existingClaims = await _userManager.GetClaimsAsync(adminUser);
        foreach (var permission in Permissions)
        {
            var hasClaim = existingClaims.Any(c => 
                c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase) && 
                c.Value.Equals(permission, StringComparison.Ordinal));

            if (!hasClaim)
            {
                _logger.LogInformation("Assigning permission claim '{Permission}' to user '{UserName}'...", permission, userName);
                try
                {
                    var result = await _userManager.AddClaimAsync(adminUser, new Claim("permission", permission));
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to assign permission '{permission}' to user '{userName}': {errors}");
                    }
                }
                catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
                {
                    var freshClaims = await _userManager.GetClaimsAsync(adminUser);
                    var verifyClaim = freshClaims.Any(c => 
                        c.Type.Equals("permission", StringComparison.OrdinalIgnoreCase) && 
                        c.Value.Equals(permission, StringComparison.Ordinal));
                    if (!verifyClaim)
                    {
                        throw;
                    }
                }
            }
        }

        _logger.LogInformation("Database seeding process completed successfully.");
    }
}
