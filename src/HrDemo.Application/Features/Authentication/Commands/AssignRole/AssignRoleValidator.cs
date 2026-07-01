using FluentValidation;

namespace HrDemo.Application.Features.Authentication.Commands.AssignRole;

public sealed class AssignRoleValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User Id is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role name is required.");
    }
}
