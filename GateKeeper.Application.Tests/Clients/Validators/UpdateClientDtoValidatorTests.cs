using FluentAssertions;
using FluentValidation.TestHelper;
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Application.Clients.Validators;

namespace GateKeeper.Application.Tests.Clients.Validators;

/// <summary>
/// Tests for UpdateClientDtoValidator.
/// </summary>
public class UpdateClientDtoValidatorTests
{
    private readonly UpdateClientDtoValidator _validator;

    public UpdateClientDtoValidatorTests()
    {
        _validator = new UpdateClientDtoValidator();
    }

    #region Valid Data Tests

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = "Updated Application",
            RedirectUris = new List<string> { "https://example.com/callback" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region DisplayName Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyDisplayName_ShouldHaveValidationError(string displayName)
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = displayName,
            RedirectUris = new List<string> { "https://example.com/callback" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name is required");
    }

    [Fact]
    public void Validate_WithTooLongDisplayName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = new string('a', 201),
            RedirectUris = new List<string> { "https://example.com/callback" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must not exceed 200 characters");
    }

    #endregion

    #region RedirectUris Validation Tests

    [Fact]
    public void Validate_WithEmptyRedirectUris_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = "Updated App",
            RedirectUris = new List<string>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RedirectUris)
            .WithErrorMessage("At least one redirect URI is required");
    }

    [Fact]
    public void Validate_WithTooManyRedirectUris_ShouldHaveValidationError()
    {
        // Arrange
        var redirectUris = Enumerable.Range(1, 11)
            .Select(i => $"https://example{i}.com/callback")
            .ToList();

        var dto = new UpdateClientDto
        {
            DisplayName = "Updated App",
            RedirectUris = redirectUris
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RedirectUris)
            .WithErrorMessage("Maximum 10 redirect URIs allowed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyRedirectUri_ShouldHaveValidationError(string uri)
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = "Updated App",
            RedirectUris = new List<string> { uri }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("RedirectUris[0]")
            .WithErrorMessage("Redirect URI cannot be empty");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("invalid")]
    [InlineData("/relative/path")]
    public void Validate_WithInvalidRedirectUri_ShouldHaveValidationError(string uri)
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = "Updated App",
            RedirectUris = new List<string> { uri }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("RedirectUris[0]")
            .WithErrorMessage("Redirect URI must be a valid absolute URL");
    }

    [Fact]
    public void Validate_WithValidAbsoluteUris_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = "Updated App",
            RedirectUris = new List<string>
            {
                "https://example.com/callback",
                "http://localhost:3000/auth/callback",
                "https://app.example.com/oauth/callback"
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RedirectUris);
    }

    [Fact]
    public void Validate_WithMultipleRedirectUris_ShouldValidateAll()
    {
        // Arrange
        var dto = new UpdateClientDto
        {
            DisplayName = "Updated App",
            RedirectUris = new List<string>
            {
                "https://valid.com/callback",
                "invalid-uri",
                "https://another-valid.com/callback"
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("RedirectUris[1]");
        // Other URIs should be validated but we only check the invalid one
    }

    #endregion
}
