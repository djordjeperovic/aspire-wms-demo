namespace AspireWms.Api.Shared.Domain;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access Value when Result is a failure. Check IsSuccess first.");

    public Error Error => !IsSuccess 
        ? _error! 
        : throw new InvalidOperationException("Cannot access Error when Result is a success. Check IsFailure first.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess, 
        Func<Error, Task<TResult>> onFailure) =>
        IsSuccess ? await onSuccess(_value!) : await onFailure(_error!);
}

/// <summary>
/// Non-generic Result for operations that don't return a value.
/// </summary>
public sealed class Result
{
    private readonly Error? _error;

    private Result()
    {
        IsSuccess = true;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error => !IsSuccess 
        ? _error! 
        : throw new InvalidOperationException("Cannot access Error when Result is a success.");

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_error!);
}

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
    
    public static Error Validation(string code, string message) => new($"Validation.{code}", message);
    public static Error NotFound(string code, string message) => new($"NotFound.{code}", message);
    public static Error Conflict(string code, string message) => new($"Conflict.{code}", message);
}
