using FluentAssertions;
using NSubstitute;
using HrDemo.Application.Abstractions.Identity;
using HrDemo.Application.Features.Authentication.Commands.AssignClaim;
using HrDemo.Application.Common.Results;
using Xunit;

namespace HrDemo.Application.UnitTests.Features.Authentication;

public sealed class AssignClaimTests
{
    private readonly IUserManager _userManagerMock;
    private readonly AssignClaimValidator _validator;
    private readonly AssignClaimHandler _handler;

    public AssignClaimTests()
    {
        _userManagerMock = Substitute.For<IUserManager>();
        _validator = new AssignClaimValidator();
        _handler = new AssignClaimHandler(_userManagerMock);
    }

    [Fact]
    public async Task GivenEmptyClaimType_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new AssignClaimCommand(1, "", "Value");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.ClaimType));
    }

    [Fact]
    public async Task GivenEmptyClaimValue_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new AssignClaimCommand(1, "Type", "");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(command.ClaimValue));
    }

    [Fact]
    public async Task GivenInvalidUserId_WhenValidated_ShouldHaveError()
    {
        // Arrange
        var command = new AssignClaimCommand(0, "Type", "Value");

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
        var command = new AssignClaimCommand(1, "Type", "Value");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidCommand_WhenHandled_ShouldAssignClaim()
    {
        // Arrange
        var command = new AssignClaimCommand(1, "Type", "Value");
        var successResult = ResponseResult.SuccessResult("Claim assigned.");

        _userManagerMock.AddClaimAsync(command.UserId, command.ClaimType, command.ClaimValue, Arg.Any<CancellationToken>())
            .Returns(successResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _userManagerMock.Received(1).AddClaimAsync(command.UserId, command.ClaimType, command.ClaimValue, Arg.Any<CancellationToken>());
    }
}
