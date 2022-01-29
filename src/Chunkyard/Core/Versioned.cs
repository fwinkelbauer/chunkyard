namespace Chunkyard.Core;

/// <summary>
/// The simplest implementation of <see cref="IVersioned"/> which is used by
/// <see cref="DataConvert"/> to deserialize versioned objects.
/// </summary>
public class Versioned : IVersioned
{
    public Versioned(int schemaVersion)
    {
        SchemaVersion = schemaVersion;
    }

    public int SchemaVersion { get; }

    public override bool Equals(object? obj)
    {
        return obj is Versioned other
            && SchemaVersion == other.SchemaVersion;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SchemaVersion);
    }
}
