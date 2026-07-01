using FluentValidation;

namespace HrDemo.Application.Features.Authentication.Commands.AssignClaim;

public sealed class AssignClaimValidator : AbstractValidator<AssignClaimCommand>
{
    public AssignClaimValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User Id is required.");

        RuleFor(x => x.ClaimType)
            .NotEmpty().WithMessage("Claim Type is required.");

        RuleFor(x => x.ClaimValue)
            .NotEmpty().WithMessage("Claim Value is required.");
    }
}
