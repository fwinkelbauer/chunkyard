namespace Chunkyard.Core;

/// <summary>
/// A reference which can be used to retrieve a set of chunks from a
/// <see cref="ChunkStore"/> based on a password based encryption key.
/// </summary>
public class LogReference
{
    public LogReference(
        byte[] salt,
        int iterations,
        IReadOnlyCollection<string> chunkIds)
    {
        Salt = salt;
        Iterations = iterations;
        ChunkIds = chunkIds;
    }

    public byte[] Salt { get; }

    public int Iterations { get; }

    public IReadOnlyCollection<string> ChunkIds { get; }

    public override bool Equals(object? obj)
    {
        return obj is LogReference other
            && Salt.SequenceEqual(other.Salt)
            && Iterations == other.Iterations
            && ChunkIds.SequenceEqual(other.ChunkIds);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Salt, Iterations, ChunkIds);
    }
}
