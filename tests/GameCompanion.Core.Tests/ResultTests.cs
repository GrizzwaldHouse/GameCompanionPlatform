using FluentAssertions;
using GameCompanion.Core.Models;
using Xunit;

namespace GameCompanion.Core.Tests;

public sealed class ResultTests
{
    // --- Non-generic Result ---

    [Fact]
    public void Success_ShouldBeSuccessful()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldContainError()
    {
        var result = Result.Failure("something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("something went wrong");
    }

    [Fact]
    public void ImplicitConversionFromString_ShouldCreateFailure()
    {
        Result result = "file not found";

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("file not found");
    }

    // --- Generic Result<T> ---

    [Fact]
    public void GenericSuccess_ShouldContainValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void GenericFailure_ShouldNotContainValue()
    {
        var result = Result<int>.Failure("parse error");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("parse error");
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Success(5);

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnFailure_ShouldPropagateError()
    {
        var result = Result<int>.Failure("bad input");

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be("bad input");
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChainResults()
    {
        var result = Result<int>.Success(10);

        var bound = result.Bind(x =>
            x > 0
                ? Result<string>.Success($"positive: {x}")
                : Result<string>.Failure("must be positive"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("positive: 10");
    }

    [Fact]
    public void Bind_OnFailure_ShouldShortCircuit()
    {
        var result = Result<int>.Failure("upstream error");

        var bound = result.Bind(x => Result<string>.Success($"value: {x}"));

        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be("upstream error");
    }

    [Fact]
    public void Match_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result<int>.Success(7);

        var output = result.Match(
            onSuccess: v => $"got {v}",
            onFailure: e => $"error: {e}");

        output.Should().Be("got 7");
    }

    [Fact]
    public void Match_OnFailure_ShouldCallFailureHandler()
    {
        var result = Result<int>.Failure("oops");

        var output = result.Match(
            onSuccess: v => $"got {v}",
            onFailure: e => $"error: {e}");

        output.Should().Be("error: oops");
    }

    [Fact]
    public void OnSuccess_ShouldExecuteAction_WhenSuccessful()
    {
        var sideEffect = 0;
        var result = Result<int>.Success(3);

        result.OnSuccess(v => sideEffect = v);

        sideEffect.Should().Be(3);
    }

    [Fact]
    public void OnSuccess_ShouldNotExecuteAction_WhenFailed()
    {
        var sideEffect = 0;
        var result = Result<int>.Failure("nope");

        result.OnSuccess(v => sideEffect = v);

        sideEffect.Should().Be(0);
    }

    [Fact]
    public void OnFailure_ShouldExecuteAction_WhenFailed()
    {
        var capturedError = "";
        var result = Result<int>.Failure("broken");

        result.OnFailure(e => capturedError = e);

        capturedError.Should().Be("broken");
    }

    [Fact]
    public void OnFailure_ShouldNotExecuteAction_WhenSuccessful()
    {
        var capturedError = "";
        var result = Result<int>.Success(1);

        result.OnFailure(e => capturedError = e);

        capturedError.Should().BeEmpty();
    }

    [Fact]
    public void ChainedOperations_ShouldComposeCorrectly()
    {
        var result = Result<string>.Success("42")
            .Map(s => int.Parse(s))
            .Map(n => n * 2)
            .Bind(n => n > 50
                ? Result<string>.Success($"big: {n}")
                : Result<string>.Failure("too small"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("big: 84");
    }

    [Fact]
    public void ChainedOperations_ShouldShortCircuitOnFirstFailure()
    {
        var mapCalled = false;

        var result = Result<int>.Failure("initial error")
            .Map(x => { mapCalled = true; return x * 2; })
            .Bind(x => Result<string>.Success($"value: {x}"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("initial error");
        mapCalled.Should().BeFalse();
    }
}
