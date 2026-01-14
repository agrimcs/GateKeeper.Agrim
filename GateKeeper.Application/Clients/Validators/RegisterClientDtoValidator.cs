using FluentValidation;
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Clients.Validators;

/// <summary>
/// Validator for client registration requests.
/// </summary>
public class RegisterClientDtoValidator : AbstractValidator<RegisterClientDto>
{
    public RegisterClientDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MaximumLength(200)
            .WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid client type");

        RuleFor(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("At least one redirect URI is required")
            .Must(uris => uris.Count <= 10)
            .WithMessage("Maximum 10 redirect URIs allowed");

        RuleForEach(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("Redirect URI cannot be empty")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Redirect URI must be a valid absolute URL");

        RuleFor(x => x.AllowedScopes)
            .NotEmpty()
            .WithMessage("At least one scope is required")
            .Must(scopes => scopes.Count <= 20)
            .WithMessage("Maximum 20 scopes allowed");

        RuleForEach(x => x.AllowedScopes)
            .NotEmpty()
            .WithMessage("Scope cannot be empty")
            .MaximumLength(100)
            .WithMessage("Scope must not exceed 100 characters");
    }
}
