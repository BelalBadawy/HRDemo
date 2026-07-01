using FluentAssertions;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.Register;
using HrDemo.Application.Common.Results;
using Xunit;

namespace HrDemo.Application.UnitTests.Features.Authentication;

public sealed class RegisterTests
{
    private readonly IUserManager _userManagerMock;
    private readonly RegisterValidator _validator;
    private readonly RegisterHandler _handler;

    public RegisterTests()
    {
        _userManagerMock = Substitute.For<IUserManager>();
        _validator = new RegisterValidator(_userManagerMock);
        _handler = new RegisterHandler(_userManagerMock);
    }

    [Fact]
    public async Task GivenValidRequest_WhenValidated_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new RegisterCommand("TestUser", "test@test.com", "Password123!");
        _userManagerMock.IsUserNameUniqueAsync(command.UserName, Arg.Any<CancellationToken>()).Returns(true);
        _userManagerMock.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenDuplicateEmail_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand("TestUser", "duplicate@test.com", "Password123!");
        _userManagerMock.IsUserNameUniqueAsync(command.UserName, Arg.Any<CancellationToken>()).Returns(true);
        _userManagerMock.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.Email));
    }

    [Fact]
    public async Task GivenDuplicateUserName_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterCommand("duplicateUser", "test@test.com", "Password123!");
        _userManagerMock.IsUserNameUniqueAsync(command.UserName, Arg.Any<CancellationToken>()).Returns(false);
        _userManagerMock.IsEmailUniqueAsync(command.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.UserName));
    }

    [Fact]
    public async Task GivenValidCommand_WhenHandled_ShouldCreateUser()
    {
        // Arrange
        var command = new RegisterCommand("TestUser", "test@test.com", "Password123!");
        var expectedUserId = 42;
        var successResult = ResponseResult<int>.SuccessResult(expectedUserId, "User created successfully.", 201);

        _userManagerMock.CreateUserAsync(command.UserName, command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedUserId);
        result.StatusCode.Should().Be(201);
    }
}
