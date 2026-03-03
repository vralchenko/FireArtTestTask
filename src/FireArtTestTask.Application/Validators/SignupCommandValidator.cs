using FireArtTestTask.Application.Auth.Commands;
using FluentValidation;

namespace FireArtTestTask.Application.Validators;

public class SignupCommandValidator : AbstractValidator<SignupCommand>
{
    public SignupCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
