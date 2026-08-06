namespace NutriTrack.Shared.Common;

/// <summary>
/// The outcome of an operation that can fail in expected ways, without a return value.
/// </summary>
/// <remarks>
/// Used for the auth paths where failure is an ordinary outcome rather than a fault. Exceptions
/// remain the mechanism for genuine faults and for the rest of the codebase.
/// </remarks>
public readonly struct Result
{
    public Error? Error { get; }

    private Result(Error? error) => Error = error;

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => new(error);
}

/// <summary>
/// The outcome of an operation that yields a <typeparamref name="T"/> or an expected failure.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;

    public Error? Error { get; }

    private Result(T value)
    {
        _value = value;
        Error = null;
    }

    private Result(Error error)
    {
        _value = default;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    /// <summary>The value. Only valid when <see cref="IsSuccess"/>.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot read Value of a failed result ({Error!.Code}).");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error!);
}
