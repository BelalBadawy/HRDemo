using FluentAssertions;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.Logout;
using HrDemo.Application.Common.Results;
using Xunit;

namespace HrDemo.Application.UnitTests.Features.Authentication;

public sealed class LogoutTests
{
    private readonly IRefreshTokenService _refreshTokenServiceMock;
    private readonly LogoutValidator _validator;
    private readonly LogoutHandler _handler;

    public LogoutTests()
    {
        _refreshTokenServiceMock = Substitute.For<IRefreshTokenService>();
        _validator = new LogoutValidator();
        _handler = new LogoutHandler(_refreshTokenServiceMock);
    }

    [Fact]
    public async Task GivenEmptyRefreshToken_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new LogoutCommand("");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.RefreshToken));
    }

    [Fact]
    public async Task GivenValidRequest_WhenValidated_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new LogoutCommand("SomeToken");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidToken_WhenHandled_ShouldCallRevoke()
    {
        // Arrange
        var command = new LogoutCommand("SomeToken", "127.0.0.1");
        var successResult = ResponseResult.SuccessResult("Logged out successfully.");

        _refreshTokenServiceMock.RevokeTokenAsync(command.RefreshToken, command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _refreshTokenServiceMock.Received(1).RevokeTokenAsync(command.RefreshToken, command.IpAddress, Arg.Any<CancellationToken>());
    }
}
