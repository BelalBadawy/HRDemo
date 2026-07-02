using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using HrDemo.Application.Abstractions.Authentication;
using HrDemo.Application.Abstractions.DateTime;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Common.Results;
using HrDemo.Application.Features.Authentication.Dtos;
using HrDemo.Infrastructure.Identity;
using HrDemo.Infrastructure.Persistence;
using HrDemo.Infrastructure.Services;
using Xunit;

namespace HrDemo.Infrastructure.IntegrationTests;

public sealed class IdentityServiceTests : IAsyncLifetime
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;
    private readonly IClock _clockMock;
    private readonly TimeProvider _timeProviderMock;
    private readonly ICurrentUser _currentUserMock;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserManager _userManagerService;
    private readonly IRefreshTokenService _refreshTokenService;

    public IdentityServiceTests()
    {
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database=HrDemoDb_Integration_Identity_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";

        var now = DateTimeOffset.UtcNow;
        _clockMock = Substitute.For<IClock>();
        _clockMock.UtcNow.Returns(now);

        _timeProviderMock = Substitute.For<TimeProvider>();
        _timeProviderMock.GetUtcNow().Returns(now);

        _currentUserMock = Substitute.For<ICurrentUser>();
        _currentUserMock.IpAddress.Returns("192.168.1.100");

        var services = new ServiceCollection();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SigningKey", "super-secret-key-that-is-long-enough-for-sha-256-encryption-key-must-be-long-enough" },
                { "Jwt:Issuer", "HrDemo" },
                { "Jwt:Audience", "HrDemoClients" },
                { "Jwt:AccessTokenLifetime", "00:15:00" },
                { "Jwt:RefreshTokenLifetime", "07.00:00:00" } // 7 days
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDataProtection();
        services.AddLogging();
        services.AddHttpContextAccessor();

        // Mocks
        services.AddSingleton(_clockMock);
        services.AddSingleton(_timeProviderMock);
        services.AddSingleton(_currentUserMock);

        // Real Token Generator
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(_connectionString));

        // Identity with full configuration to support SignInManager
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // Services
        services.AddScoped<IUserManager, UserManagerService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _userManagerService = _serviceProvider.GetRequiredService<IUserManager>();
        _refreshTokenService = _serviceProvider.GetRequiredService<IRefreshTokenService>();
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
    public async Task LoginAsync_HappyPath_ShouldSucceed()
    {
        // Arrange
        var username = "user1";
        var email = "user1@test.com";
        var password = "Password123!";
        
        var createResult = await _userManagerService.CreateUserAsync(username, email, password);
        createResult.Success.Should().BeTrue();

        // Act
        var loginResult = await _userManagerService.LoginAsync(username, password, CancellationToken.None);

        // Assert
        loginResult.Success.Should().BeTrue();
        loginResult.Data.Should().NotBeNull();
        loginResult.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginResult.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Assert CreatedByIp is captured correctly from ICurrentUser
        var dbUser = await _userManager.FindByNameAsync(username);
        dbUser.Should().NotBeNull();
        var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == dbUser!.Id);
        tokenRecord.Should().NotBeNull();
        tokenRecord!.CreatedByIp.Should().Be("192.168.1.100");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ShouldReturnAccountIsInactive()
    {
        // Arrange
        var username = "inactiveuser";
        var email = "inactive@test.com";
        var password = "Password123!";

        var createResult = await _userManagerService.CreateUserAsync(username, email, password);
        createResult.Success.Should().BeTrue();

        // Deactivate user
        var user = await _userManager.FindByNameAsync(username);
        user!.IsActive = false;
        await _userManager.UpdateAsync(user);

        // Act
        var loginResult = await _userManagerService.LoginAsync(username, password, CancellationToken.None);

        // Assert
        loginResult.Success.Should().BeFalse();
        loginResult.Status.Should().Be(ResultStatus.Unauthorized);
        loginResult.StatusCode.Should().Be(401);
        loginResult.Message.Should().Be("Account is inactive.");
    }

    [Fact]
    public async Task LoginAsync_LockoutFlow_ShouldLockAccountOnFifthFailedAttempt()
    {
        // Arrange
        var username = "lockoutuser";
        var email = "lockout@test.com";
        var password = "Password123!";

        var createResult = await _userManagerService.CreateUserAsync(username, email, password);
        createResult.Success.Should().BeTrue();

        // Act: first 4 failed attempts should fail with normal invalid credentials
        for (int i = 0; i < 4; i++)
        {
            var failedResult = await _userManagerService.LoginAsync(username, "WrongPassword", CancellationToken.None);
            failedResult.Success.Should().BeFalse();
            failedResult.Message.Should().Be("Invalid username or password.");
        }

        // Act: 5th attempt triggers lockout and returns "Account is locked."
        var fifthResult = await _userManagerService.LoginAsync(username, "WrongPassword", CancellationToken.None);
        fifthResult.Success.Should().BeFalse();
        fifthResult.Message.Should().Be("Account is locked.");

        // Act: 6th attempt (even with CORRECT password) should be locked out
        var lockoutResult = await _userManagerService.LoginAsync(username, password, CancellationToken.None);

        // Assert
        lockoutResult.Success.Should().BeFalse();
        lockoutResult.Status.Should().Be(ResultStatus.Unauthorized);
        lockoutResult.StatusCode.Should().Be(401);
        lockoutResult.Message.Should().Be("Account is locked.");
    }

    [Fact]
    public async Task LoginAsync_ExpiredLockout_ShouldSucceed()
    {
        // Arrange
        var username = "expiredlockoutuser";
        var email = "expiredlockout@test.com";
        var password = "Password123!";
        var baseTime = DateTimeOffset.UtcNow;

        _timeProviderMock.GetUtcNow().Returns(baseTime);
        _clockMock.UtcNow.Returns(baseTime);

        var createResult = await _userManagerService.CreateUserAsync(username, email, password);
        createResult.Success.Should().BeTrue();

        // Trigger lockout (5 failures)
        for (int i = 0; i < 5; i++)
        {
            await _userManagerService.LoginAsync(username, "WrongPassword", CancellationToken.None);
        }

        // Verify currently locked
        var currentLockResult = await _userManagerService.LoginAsync(username, password, CancellationToken.None);
        currentLockResult.Message.Should().Be("Account is locked.");

        // Simulate time advance by manually updating LockoutEnd in the database to a past time
        var dbUser = await _userManager.FindByNameAsync(username);
        dbUser.Should().NotBeNull();
        dbUser!.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-5);
        await _userManager.UpdateAsync(dbUser);

        // Act: Attempt login again after lockout time expired
        var loginResult = await _userManagerService.LoginAsync(username, password, CancellationToken.None);

        // Assert
        loginResult.Success.Should().BeTrue();
        loginResult.Data.Should().NotBeNull();
        loginResult.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RotateTokenAsync_InactiveUser_ShouldFailAndRemoveToken()
    {
        // Arrange
        var username = "inactiveforrefresh";
        var email = "inactiveforrefresh@test.com";
        var password = "Password123!";

        var createResult = await _userManagerService.CreateUserAsync(username, email, password);
        createResult.Success.Should().BeTrue();

        // Do login to create a refresh token
        var loginResult = await _userManagerService.LoginAsync(username, password, CancellationToken.None);
        loginResult.Success.Should().BeTrue();
        var originalRefreshToken = loginResult.Data!.RefreshToken;

        // Deactivate user
        var user = await _userManager.FindByNameAsync(username);
        user!.IsActive = false;
        await _userManager.UpdateAsync(user);

        // Act: Attempt to rotate token for deactivated user
        var rotateResult = await _refreshTokenService.RotateTokenAsync(originalRefreshToken, CancellationToken.None);

        // Assert
        rotateResult.Success.Should().BeFalse();
        rotateResult.Status.Should().Be(ResultStatus.Unauthorized);
        rotateResult.StatusCode.Should().Be(401);
        rotateResult.Message.Should().Be("Account is inactive.");

        // Assert refresh token record was DELETED
        var dbUser = await _userManager.FindByNameAsync(username);
        var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == dbUser!.Id);
        tokenRecord.Should().BeNull();
    }

    [Fact]
    public void LoginCommand_ShouldNotHaveIpAddressProperty()
    {
        // Assert compile-level expectation: LoginCommand properties
        var type = typeof(HrDemo.Application.Features.Authentication.Commands.Login.LoginCommand);
        var ipProp = type.GetProperty("IpAddress");
        ipProp.Should().BeNull("IpAddress must not be a property of LoginCommand");
    }
}
