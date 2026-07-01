using FluentAssertions;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.AssignRole;
using HrDemo.Application.Common.Results;
using Xunit;

namespace HrDemo.Application.UnitTests.Features.Authentication;

public sealed class AssignRoleTests
{
    private readonly IUserManager _userManagerMock;
    private readonly AssignRoleValidator _validator;
    private readonly AssignRoleHandler _handler;

    public AssignRoleTests()
    {
        _userManagerMock = Substitute.For<IUserManager>();
        _validator = new AssignRoleValidator();
        _handler = new AssignRoleHandler(_userManagerMock);
    }

    [Fact]
    public async Task GivenEmptyRole_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new AssignRoleCommand(1, "");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.Role));
    }

    [Fact]
    public async Task GivenInvalidUserId_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new AssignRoleCommand(0, "Admin");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.UserId));
    }

    [Fact]
    public async Task GivenValidRequest_WhenValidated_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new AssignRoleCommand(1, "Admin");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidCommand_WhenHandled_ShouldAssignRole()
    {
        // Arrange
        var command = new AssignRoleCommand(1, "Admin");
        var successResult = ResponseResult.SuccessResult("Role assigned.");

        _userManagerMock.AddToRoleAsync(command.UserId, command.Role, Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _userManagerMock.Received(1).AddToRoleAsync(command.UserId, command.Role, Arg.Any<CancellationToken>());
    }
}
