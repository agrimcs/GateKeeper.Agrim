namespace GateKeeper.Application.Common.Models;

/// <summary>
/// Represents the result of an operation with success/failure state.
/// Useful for operations that may fail without exceptions.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Generic result with return value.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }
    
    private Result(bool isSuccess, T? value, string? error) 
        : base(isSuccess, error)
    {
        Value = value;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error);
}
