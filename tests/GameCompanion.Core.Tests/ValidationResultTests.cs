using FluentAssertions;
using GameCompanion.Core.Models;
using Xunit;

namespace GameCompanion.Core.Tests;

public sealed class ValidationResultTests
{
    [Fact]
    public void Valid_ShouldCreateValidResult()
    {
        var result = ValidationResult.Valid();

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Invalid_ShouldCreateInvalidResultWithMessage()
    {
        var result = ValidationResult.Invalid("Value must be positive");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Value must be positive");
    }

    [Fact]
    public void Invalid_WithEmptyMessage_ShouldStillBeInvalid()
    {
        var result = ValidationResult.Invalid("");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void ImplicitConversionToBool_WhenValid_ShouldReturnTrue()
    {
        ValidationResult result = ValidationResult.Valid();

        bool isValid = result;

        isValid.Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversionToBool_WhenInvalid_ShouldReturnFalse()
    {
        ValidationResult result = ValidationResult.Invalid("error");

        bool isValid = result;

        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidationInCondition_WhenValid_ShouldExecuteTrueBranch()
    {
        var result = ValidationResult.Valid();
        var branchExecuted = false;

        if (result)
        {
            branchExecuted = true;
        }

        branchExecuted.Should().BeTrue();
    }

    [Fact]
    public void ValidationInCondition_WhenInvalid_ShouldExecuteFalseBranch()
    {
        var result = ValidationResult.Invalid("error");
        var falseBranchExecuted = false;

        if (!result)
        {
            falseBranchExecuted = true;
        }

        falseBranchExecuted.Should().BeTrue();
    }

    [Fact]
    public void MultipleValidations_ShouldAllowChaining()
    {
        var validations = new[]
        {
            ValidationResult.Valid(),
            ValidationResult.Valid(),
            ValidationResult.Invalid("Third validation failed")
        };

        var firstFailure = validations.FirstOrDefault(v => !v.IsValid);

        firstFailure.Should().NotBeNull();
        firstFailure!.ErrorMessage.Should().Be("Third validation failed");
    }
}
