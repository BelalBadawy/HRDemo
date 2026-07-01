using FluentAssertions;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.Login;
using HrDemo.Application.Features.Authentication.Dtos;
using HrDemo.Application.Common.Results;
using Xunit;

namespace HrDemo.Application.UnitTests.Features.Authentication;

public sealed class LoginTests
{
    private readonly IUserManager _userManagerMock;
    private readonly LoginValidator _validator;
    private readonly LoginHandler _handler;

    public LoginTests()
    {
        _userManagerMock = Substitute.For<IUserManager>();
        _validator = new LoginValidator();
        _handler = new LoginHandler(_userManagerMock);
    }

    [Fact]
    public async Task GivenEmptyUserName_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("", "Password123!");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.UserNameOrEmail));
    }

    [Fact]
    public async Task GivenEmptyPassword_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("testuser", "");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.Password));
    }

    [Fact]
    public async Task GivenValidRequest_WhenValidated_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password123!");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidCredentials_WhenHandled_ShouldReturnToken()
    {
        // Arrange
        var command = new LoginCommand("testuser", "Password123!");
        var expectedResponse = new LoginResponseDto
        {
            AccessToken = "Access.Token.Here",
            RefreshToken = "Refresh.Token.Here"
        };
        var successResult = ResponseResult<LoginResponseDto>.SuccessResult(expectedResponse, "Login successful.");

        _userManagerMock.LoginAsync(command.UserNameOrEmail, command.Password, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("Access.Token.Here");
        result.Data.RefreshToken.Should().Be("Refresh.Token.Here");
    }

    [Fact]
    public async Task GivenInvalidCredentials_WhenHandled_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("testuser", "WrongPassword");
        var failureResult = ResponseResult<LoginResponseDto>.FailureResult(ResultStatus.Unauthorized, "Invalid credentials.", 401);

        _userManagerMock.LoginAsync(command.UserNameOrEmail, command.Password, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(failureResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.StatusCode.Should().Be(401);
    }
}
