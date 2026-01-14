using FluentValidation;
using GateKeeper.Application.Clients.DTOs;

namespace GateKeeper.Application.Clients.Validators;

/// <summary>
/// Validator for client update requests.
/// </summary>
public class UpdateClientDtoValidator : AbstractValidator<UpdateClientDto>
{
    public UpdateClientDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MaximumLength(200)
            .WithMessage("Display name must not exceed 200 characters");

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
    }
}
