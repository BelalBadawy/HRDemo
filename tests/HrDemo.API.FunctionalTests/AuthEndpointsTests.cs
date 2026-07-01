using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.Register;
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
}
