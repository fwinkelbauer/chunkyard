namespace Chunkyard.Core;

/// <summary>
/// Describes meta data of a binary data blob.
/// </summary>
public sealed class Blob
{
    public Blob(string name, DateTime lastWriteTimeUtc)
    {
        Name = name;
        LastWriteTimeUtc = Standardize(lastWriteTimeUtc);
    }

    public string Name { get; }

    public DateTime LastWriteTimeUtc { get; }

    public override bool Equals(object? obj)
    {
        return obj is Blob other
            && Name == other.Name
            && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Name,
            LastWriteTimeUtc);
    }

    // Some file systems have a limit on the last write time property, so we'll
    // unify all Blobs
    private static DateTime Standardize(DateTime date)
    {
        return new DateTime(
            date.Year,
            date.Month,
            date.Day,
            date.Hour,
            date.Minute,
            date.Second);
    }
}
