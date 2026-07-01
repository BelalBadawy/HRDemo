using FluentAssertions;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.Refresh;
using HrDemo.Application.Features.Authentication.Dtos;
using HrDemo.Application.Common.Results;
using Xunit;

namespace HrDemo.Application.UnitTests.Features.Authentication;

public sealed class RefreshTests
{
    private readonly IRefreshTokenService _refreshTokenServiceMock;
    private readonly RefreshValidator _validator;
    private readonly RefreshHandler _handler;

    public RefreshTests()
    {
        _refreshTokenServiceMock = Substitute.For<IRefreshTokenService>();
        _validator = new RefreshValidator();
        _handler = new RefreshHandler(_refreshTokenServiceMock);
    }

    [Fact]
    public async Task GivenEmptyRefreshToken_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshCommand("");

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
        var command = new RefreshCommand("SomeToken");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidToken_WhenHandled_ShouldReturnNewTokens()
    {
        // Arrange
        var command = new RefreshCommand("OldToken", "127.0.0.1");
        var expectedResponse = new LoginResponseDto
        {
            AccessToken = "NewAccessToken",
            RefreshToken = "NewRefreshToken"
        };
        var successResult = ResponseResult<LoginResponseDto>.SuccessResult(expectedResponse, "Token refreshed.");

        _refreshTokenServiceMock.RotateTokenAsync(command.RefreshToken, command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("NewAccessToken");
        result.Data.RefreshToken.Should().Be("NewRefreshToken");
    }
}
