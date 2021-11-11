namespace Chunkyard.Core;

/// <summary>
/// A custom exception type.
/// </summary>
[Serializable]
public class ChunkyardException : Exception
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

    protected ChunkyardException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
    }
}
