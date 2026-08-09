namespace Chunkyard.Core;

/// <summary>
/// Describes meta data of a binary data blob.
/// </summary>
public sealed record Blob(string Name, DateTime LastWriteTimeUtc);
