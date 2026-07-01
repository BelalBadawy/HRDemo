using FluentValidation;
using HrDemo.Application.Abstractions.Identity;

namespace HrDemo.Application.Features.Authentication.Commands.Register;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator(IUserManager userManager)
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.")
            .MustAsync(async (userName, cancellationToken) => 
                await userManager.IsUserNameUniqueAsync(userName, cancellationToken))
            .WithMessage("Username is already taken.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MustAsync(async (email, cancellationToken) => 
                await userManager.IsEmailUniqueAsync(email, cancellationToken))
            .WithMessage("Email is already registered.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
