namespace Chunkyard.Core;

/// <summary>
/// A custom exception type.
/// </summary>
public sealed class ChunkyardException : Exception
{
    public ChunkyardException()
    {
    }

    public ChunkyardException(string message)
        : base(message)
    {
    }

    public ChunkyardException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
