using FluentAssertions;
using FluentValidation.TestHelper;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Validators;

namespace GateKeeper.Application.Tests.Users.Validators;

/// <summary>
/// Tests for UpdateUserProfileDtoValidator.
/// </summary>
public class UpdateUserProfileDtoValidatorTests
{
    private readonly UpdateUserProfileDtoValidator _validator;

    public UpdateUserProfileDtoValidatorTests()
    {
        _validator = new UpdateUserProfileDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new UpdateUserProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyFirstName_ShouldHaveValidationError(string firstName)
    {
        // Arrange
        var dto = new UpdateUserProfileDto
        {
            FirstName = firstName,
            LastName = "Smith"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyLastName_ShouldHaveValidationError(string lastName)
    {
        // Arrange
        var dto = new UpdateUserProfileDto
        {
            FirstName = "Jane",
            LastName = lastName
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name is required");
    }

    [Fact]
    public void Validate_WithTooLongFirstName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateUserProfileDto
        {
            FirstName = new string('a', 101),
            LastName = "Smith"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithTooLongLastName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateUserProfileDto
        {
            FirstName = "Jane",
            LastName = new string('a', 101)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name must not exceed 100 characters");
    }
}
