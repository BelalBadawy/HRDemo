using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.Register;
using HrDemo.Application.Features.Authentication.Commands.Login;
using HrDemo.Application.Features.Authentication.Commands.Refresh;
using HrDemo.Application.Features.Authentication.Commands.Logout;
using HrDemo.Application.Features.Authentication.Dtos;
using HrDemo.Application.Common.Results;
using Xunit;
using FluentAssertions;

namespace HrDemo.API.FunctionalTests;

public sealed class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GivenValidRegistration_WhenPosted_ShouldReturnCreated()
    {
        // Arrange
        var userManagerMock = Substitute.For<IUserManager>();
        userManagerMock.IsUserNameUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        userManagerMock.IsEmailUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        userManagerMock.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResponseResult<int>.SuccessResult(1, "User created successfully.", 201));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => userManagerMock);
            });
        }).CreateClient();

        var command = new RegisterCommand("NewUser", "newuser@test.com", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ResponseResult<int>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Be(1);
    }

    [Fact]
    public async Task GivenInvalidRegistration_WhenPosted_ShouldReturnBadRequest()
    {
        // Arrange
        var userManagerMock = Substitute.For<IUserManager>();
        // Simulate duplicate username
        userManagerMock.IsUserNameUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        userManagerMock.IsEmailUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => userManagerMock);
            });
        }).CreateClient();

        var command = new RegisterCommand("DuplicateUser", "newuser@test.com", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/register", UriKind.Relative), command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ResponseResult<int>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.ValidationError);
        result.Errors.Should().ContainKey(nameof(command.UserName));
    }

    [Fact]
    public async Task GivenValidLogin_WhenPosted_ShouldReturnOk()
    {
        // Arrange
        var userManagerMock = Substitute.For<IUserManager>();
        var expectedResponse = new LoginResponseDto
        {
            AccessToken = "valid-access-token",
            RefreshToken = "valid-refresh-token"
        };
        userManagerMock.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResponseResult<LoginResponseDto>.SuccessResult(expectedResponse, "Login successful."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => userManagerMock);
            });
        }).CreateClient();

        var command = new LoginCommand("testuser", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResponseResult<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("valid-access-token");
        result.Data.RefreshToken.Should().Be("valid-refresh-token");
    }

    [Fact]
    public async Task GivenInvalidLogin_WhenPosted_ShouldReturnUnauthorized()
    {
        // Arrange
        var userManagerMock = Substitute.For<IUserManager>();
        userManagerMock.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResponseResult<LoginResponseDto>.FailureResult(ResultStatus.Unauthorized, "Invalid credentials.", 401));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => userManagerMock);
            });
        }).CreateClient();

        var command = new LoginCommand("testuser", "WrongPassword");

        // Act
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ResponseResult<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task GivenValidRefreshToken_WhenPosted_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshTokenServiceMock = Substitute.For<IRefreshTokenService>();
        var expectedResponse = new LoginResponseDto
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token"
        };
        refreshTokenServiceMock.RotateTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResponseResult<LoginResponseDto>.SuccessResult(expectedResponse, "Token refreshed."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => refreshTokenServiceMock);
            });
        }).CreateClient();

        var command = new RefreshCommand("OldRefreshToken");

        // Act
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/refresh", UriKind.Relative), command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResponseResult<LoginResponseDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task GivenValidLogout_WhenPosted_ShouldReturnOk()
    {
        // Arrange
        var refreshTokenServiceMock = Substitute.For<IRefreshTokenService>();
        refreshTokenServiceMock.RevokeTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ResponseResult.SuccessResult("Logged out."));

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => refreshTokenServiceMock);
            });
        }).CreateClient();

        var command = new LogoutCommand("SomeRefreshToken");

        // Act
        var response = await client.PostAsJsonAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ResponseResult>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}
