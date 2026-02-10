namespace GameCompanion.Core.Interfaces;

/// <summary>
/// Represents the result of an operation that may succeed or fail.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    string? Error { get; }
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
public interface IResult<out T> : IResult
{
    T? Value { get; }
}
