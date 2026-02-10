using FluentAssertions;
using GameCompanion.Core.Models;
using Xunit;

namespace GameCompanion.Core.Tests;

/// <summary>
/// Additional tests focused on error handling and reporting scenarios.
/// </summary>
public sealed class ResultErrorHandlingTests
{
    // Test error propagation through multiple layers
    [Fact]
    public void MultiLayerErrorPropagation_ShouldPreserveOriginalError()
    {
        var initialError = "Database connection failed";
        var result = Result<int>.Failure(initialError)
            .Map(x => x * 2)
            .Map(x => x + 10)
            .Bind(x => Result<string>.Success($"Value: {x}"))
            .Map(s => s.ToUpper());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(initialError);
    }

    // Test error handling with null values
    [Fact]
    public void ResultWithNullError_ShouldStillBeFailure()
    {
        // Using the internal constructor through Failure method
        var result = Result<int>.Failure(null!);

        result.IsFailure.Should().BeTrue();
    }

    // Test error reporting with complex types
    [Fact]
    public void ResultWithComplexType_OnFailure_ShouldHandleCorrectly()
    {
        var errorMessage = "Failed to parse configuration";
        var result = Result<Dictionary<string, object>>.Failure(errorMessage);

        var capturedError = "";
        result.OnFailure(e => capturedError = e);

        capturedError.Should().Be(errorMessage);
    }

    // Test chaining OnSuccess and OnFailure
    [Fact]
    public void ChainingSuccessAndFailureHandlers_ShouldExecuteAppropriateHandlers()
    {
        var successCalled = false;
        var failureCalled = false;

        Result<int>.Failure("error")
            .OnSuccess(_ => successCalled = true)
            .OnFailure(_ => failureCalled = true);

        successCalled.Should().BeFalse();
        failureCalled.Should().BeTrue();
    }

    // Test error accumulation pattern
    [Fact]
    public void AccumulatingErrors_ThroughSequentialOperations()
    {
        var errors = new List<string>();

        var result1 = Result<int>.Failure("Error 1");
        result1.OnFailure(e => errors.Add(e));

        var result2 = Result<int>.Failure("Error 2");
        result2.OnFailure(e => errors.Add(e));

        var result3 = Result<int>.Success(42);
        result3.OnFailure(e => errors.Add(e));

        errors.Should().HaveCount(2);
        errors.Should().ContainInOrder("Error 1", "Error 2");
    }

    // Test error handling in async-like scenarios
    [Fact]
    public void SimulatingAsyncErrorHandling_WithResult()
    {
        static Result<int> FetchData() => Result<int>.Failure("Network timeout");
        static Result<string> ProcessData(int data) => Result<string>.Success($"Processed: {data}");

        var result = FetchData()
            .Bind(ProcessData)
            .Map(s => s.ToUpper());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Network timeout");
    }

    // Test error recovery pattern
    [Fact]
    public void ErrorRecovery_UsingMatch()
    {
        var result = Result<int>.Failure("Parsing failed");

        var recoveredValue = result.Match(
            onSuccess: value => value,
            onFailure: _ => 0  // Default value on error
        );

        recoveredValue.Should().Be(0);
    }

    // Test that Match always returns a value
    [Fact]
    public void Match_ShouldAlwaysReturnValue_RegardlessOfResult()
    {
        var successResult = Result<int>.Success(42);
        var failureResult = Result<int>.Failure("error");

        var successValue = successResult.Match(
            onSuccess: v => v.ToString(),
            onFailure: e => $"Error: {e}"
        );

        var failureValue = failureResult.Match(
            onSuccess: v => v.ToString(),
            onFailure: e => $"Error: {e}"
        );

        successValue.Should().Be("42");
        failureValue.Should().Be("Error: error");
    }

    // Test implicit conversion creates failure
    [Fact]
    public void ImplicitConversionFromString_ShouldCreateFailureResult()
    {
        Result nonGeneric = "Something went wrong";

        nonGeneric.IsFailure.Should().BeTrue();
        nonGeneric.Error.Should().Be("Something went wrong");
    }

    // Test short-circuiting with multiple Binds
    [Fact]
    public void MultipleBinds_ShouldShortCircuitOnFirstFailure()
    {
        var operation2Called = false;
        var operation3Called = false;

        var result = Result<int>.Success(10)
            .Bind(x => Result<int>.Failure("Operation 1 failed"))
            .Bind(x =>
            {
                operation2Called = true;
                return Result<int>.Success(x * 2);
            })
            .Bind(x =>
            {
                operation3Called = true;
                return Result<int>.Success(x + 5);
            });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Operation 1 failed");
        operation2Called.Should().BeFalse();
        operation3Called.Should().BeFalse();
    }

    // Test error reporting with complex error messages
    [Fact]
    public void ComplexErrorMessages_ShouldBePreserved()
    {
        var errorMessage = @"Validation failed:
- Field 'Name' is required
- Field 'Age' must be positive
- Field 'Email' is not a valid email address";

        var result = Result<string>.Failure(errorMessage);

        result.Error.Should().Be(errorMessage);
    }

    // Test OnFailure returns the same result for chaining
    [Fact]
    public void OnFailure_ShouldReturnSameResult_ForChaining()
    {
        var originalResult = Result<int>.Failure("error");
        var chainedResult = originalResult.OnFailure(_ => { });

        chainedResult.IsFailure.Should().BeTrue();
        chainedResult.Error.Should().Be(originalResult.Error);
    }

    // Test OnSuccess returns the same result for chaining
    [Fact]
    public void OnSuccess_ShouldReturnSameResult_ForChaining()
    {
        var originalResult = Result<int>.Success(42);
        var chainedResult = originalResult.OnSuccess(_ => { });

        chainedResult.IsSuccess.Should().BeTrue();
        chainedResult.Value.Should().Be(originalResult.Value);
    }

    // Test implicit conversion to success
    [Fact]
    public void ImplicitConversionToSuccess_FromValue()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    // Test combining validation with Result pattern
    [Fact]
    public void CombiningValidationWithResult_Pattern()
    {
        static Result<int> ValidateAndParse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result<int>.Failure("Input cannot be empty");

            if (!int.TryParse(input, out var value))
                return Result<int>.Failure($"Cannot parse '{input}' as integer");

            if (value <= 0)
                return Result<int>.Failure("Value must be positive");

            return Result<int>.Success(value);
        }

        var emptyResult = ValidateAndParse("");
        emptyResult.IsFailure.Should().BeTrue();
        emptyResult.Error.Should().Contain("empty");

        var invalidResult = ValidateAndParse("abc");
        invalidResult.IsFailure.Should().BeTrue();
        invalidResult.Error.Should().Contain("parse");

        var negativeResult = ValidateAndParse("-5");
        negativeResult.IsFailure.Should().BeTrue();
        negativeResult.Error.Should().Contain("positive");

        var validResult = ValidateAndParse("42");
        validResult.IsSuccess.Should().BeTrue();
        validResult.Value.Should().Be(42);
    }
}
