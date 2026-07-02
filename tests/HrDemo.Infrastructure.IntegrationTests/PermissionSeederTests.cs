using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HrDemo.Infrastructure.Identity;
using HrDemo.Infrastructure.Persistence;
using Xunit;

namespace HrDemo.Infrastructure.IntegrationTests;

public sealed class PermissionSeederTests : IAsyncLifetime
{
    private static readonly string[] ExpectedPermissions = 
    {
        "roles.create",
        "roles.view",
        "roles.update",
        "roles.delete",
        "roles.list",
        "roles.assign",
        "claims.assign"
    };

    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;

    public PermissionSeederTests()
    {
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database=HrDemoDb_Integration_Seeder_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";

        var services = new ServiceCollection();

        // Register configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "DefaultAdmin:UserName", "testadmin" },
                { "DefaultAdmin:Email", "testadmin@hrdemo.com" },
                { "DefaultAdmin:Password", "AdminPassword123!" }
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDataProtection();
        services.AddLogging();
        services.AddSingleton<HrDemo.Application.Abstractions.DateTime.IClock, HrDemo.Infrastructure.Services.SystemClock>();

        // Database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(_connectionString));

        // Add Identity Core & stores
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<RoleManager<ApplicationRole>>();
        services.AddScoped<PermissionSeeder>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateRolesAdminAndUser()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<PermissionSeeder>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Act
        await seeder.SeedAsync();

        // Assert
        var adminRoleExists = await roleManager.RoleExistsAsync("Admin");
        var userRoleExists = await roleManager.RoleExistsAsync("User");

        adminRoleExists.Should().BeTrue();
        userRoleExists.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateDefaultAdminAndAssignRoleAndClaims()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<PermissionSeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Act
        await seeder.SeedAsync();

        // Assert
        var adminUser = await userManager.FindByNameAsync("testadmin");
        adminUser.Should().NotBeNull();
        adminUser!.Email.Should().Be("testadmin@hrdemo.com");

        var roles = await userManager.GetRolesAsync(adminUser);
        roles.Should().Contain("Admin");

        var claims = await userManager.GetClaimsAsync(adminUser);
        var permissions = claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

        permissions.Should().Contain(ExpectedPermissions);
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotentOnSubsequentRuns()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<PermissionSeeder>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Act - run once
        await seeder.SeedAsync();

        // Run second time
        await seeder.SeedAsync();

        // Assert - user, roles, and claims count must be correct without duplicates
        var adminUser = await userManager.FindByNameAsync("testadmin");
        adminUser.Should().NotBeNull();

        var roles = await userManager.GetRolesAsync(adminUser!);
        roles.Should().ContainSingle(r => r == "Admin");

        var claims = await userManager.GetClaimsAsync(adminUser!);
        var permissions = claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

        permissions.Should().HaveCount(7);
    }
}
