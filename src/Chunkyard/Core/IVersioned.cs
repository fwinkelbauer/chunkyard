namespace Chunkyard.Core;

/// <summary>
/// An interface to give an object an explicit schema version.
/// </summary>
public interface IVersioned
{
    int SchemaVersion { get; }
}
