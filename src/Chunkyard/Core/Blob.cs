namespace Chunkyard.Core;

/// <summary>
/// Describes meta data of a binary data blob.
/// </summary>
public sealed class Blob
{
    public Blob(string name, DateTime lastWriteTimeUtc)
    {
        Name = name;
        LastWriteTimeUtc = lastWriteTimeUtc;
    }

    public string Name { get; }

    public DateTime LastWriteTimeUtc { get; }

    public override bool Equals(object? obj)
    {
        return obj is Blob other
            && Name.Equals(other.Name)
            && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name,
            LastWriteTimeUtc);
    }
}
