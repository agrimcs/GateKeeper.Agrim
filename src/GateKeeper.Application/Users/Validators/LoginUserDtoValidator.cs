using FluentValidation;
using GateKeeper.Application.Users.DTOs;

namespace GateKeeper.Application.Users.Validators;

/// <summary>
/// Validator for user login requests.
/// </summary>
public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}
