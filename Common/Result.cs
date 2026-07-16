using System;

namespace EarthquakeNotifier.Common;

/// <summary>
/// Represents the outcome of an operation that can succeed with a value or fail with a known error.
/// </summary>
public sealed class Result<T>
{
    /// <summary>The result value. Only set when <see cref="IsSuccess"/> is <c>true</c>.</summary>
    public T? Value { get; }

    /// <summary>Human-readable error description. Only set when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; }

    /// <summary>The underlying exception, if any. Only set when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public Exception? Exception { get; }

    /// <summary><c>true</c> if the operation succeeded; <c>false</c> otherwise.</summary>
    public bool IsSuccess { get; }

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(string errorMessage, Exception? exception = null)
    {
        ErrorMessage = errorMessage;
        Exception = exception;
        IsSuccess = false;
    }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result with the given error message and optional exception.</summary>
    public static Result<T> Failure(string errorMessage, Exception? exception = null) => new(errorMessage, exception);
}

/// <summary>
/// Represents the outcome of an operation with no return value.
/// </summary>
public sealed class Result
{
    /// <summary>Human-readable error description. Only set when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; }

    /// <summary>The underlying exception, if any. Only set when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public Exception? Exception { get; }

    /// <summary><c>true</c> if the operation succeeded; <c>false</c> otherwise.</summary>
    public bool IsSuccess { get; }

    private Result() => IsSuccess = true;

    private Result(string errorMessage, Exception? exception = null)
    {
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new();

    /// <summary>Creates a failed result with the given error message and optional exception.</summary>
    public static Result Failure(string errorMessage, Exception? exception = null) => new(errorMessage, exception);
}
