namespace Chunkyard.Cli;

public static class Result
{
    public static Result<T> Success<T>(T value)
        where T : class
    {
        return new Result<T>(value, Array.Empty<string>());
    }

    public static Result<T> Error<T>(IReadOnlyCollection<string> errors)
        where T : class
    {
        return new Result<T>(null, errors);
    }

    public static Result<T> Error<T>(params string[] errors)
        where T : class
    {
        return new Result<T>(null, errors);
    }
}

public sealed class Result<T> where T : class
{
    public Result(
        T? value,
        IReadOnlyCollection<string> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public override bool Equals(object? obj)
    {
        return obj is Result<T> other
            && EqualityComparer<T?>.Default.Equals(Value, other.Value)
            && Errors.SequenceEqual(other.Errors);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Errors);
    }
}
