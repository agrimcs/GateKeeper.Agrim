using FluentAssertions;
using FluentValidation.TestHelper;
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Application.Clients.Validators;
using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Tests.Clients.Validators;

/// <summary>
/// Tests for RegisterClientDtoValidator.
/// </summary>
public class RegisterClientDtoValidatorTests
{
    private readonly RegisterClientDtoValidator _validator;

    public RegisterClientDtoValidatorTests()
    {
        _validator = new RegisterClientDtoValidator();
    }

    #region Valid Data Tests

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "My Application",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile" }
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
        var dto = new RegisterClientDto
        {
            DisplayName = displayName,
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
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
        var dto = new RegisterClientDto
        {
            DisplayName = new string('a', 201),
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must not exceed 200 characters");
    }

    #endregion

    #region ClientType Validation Tests

    [Fact]
    public void Validate_WithValidClientType_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Confidential,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    #endregion

    #region RedirectUris Validation Tests

    [Fact]
    public void Validate_WithEmptyRedirectUris_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string>(),
            AllowedScopes = new List<string> { "openid" }
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

        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = redirectUris,
            AllowedScopes = new List<string> { "openid" }
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
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { uri },
            AllowedScopes = new List<string> { "openid" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("RedirectUris[0]");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("/relative/path")]
    [InlineData("just text")]
    public void Validate_WithInvalidRedirectUri_ShouldHaveValidationError(string uri)
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { uri },
            AllowedScopes = new List<string> { "openid" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("RedirectUris[0]");
    }

    [Fact]
    public void Validate_WithValidAbsoluteUris_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> 
            { 
                "https://example.com/callback",
                "http://localhost:3000/callback"
            },
            AllowedScopes = new List<string> { "openid" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RedirectUris);
    }

    #endregion

    #region AllowedScopes Validation Tests

    [Fact]
    public void Validate_WithEmptyAllowedScopes_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string>()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AllowedScopes)
            .WithErrorMessage("At least one scope is required");
    }

    [Fact]
    public void Validate_WithTooManyScopes_ShouldHaveValidationError()
    {
        // Arrange
        var scopes = Enumerable.Range(1, 21)
            .Select(i => $"scope{i}")
            .ToList();

        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = scopes
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AllowedScopes)
            .WithErrorMessage("Maximum 20 scopes allowed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyScope_ShouldHaveValidationError(string scope)
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { scope }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("AllowedScopes[0]")
            .WithErrorMessage("Scope cannot be empty");
    }

    [Fact]
    public void Validate_WithTooLongScope_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { new string('a', 101) }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("AllowedScopes[0]")
            .WithErrorMessage("Scope must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithValidScopes_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new RegisterClientDto
        {
            DisplayName = "Test App",
            Type = ClientType.Public,
            RedirectUris = new List<string> { "https://example.com/callback" },
            AllowedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AllowedScopes);
    }

    #endregion
}
