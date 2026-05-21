namespace Chunkyard.Core;

/// <summary>
/// An interface to retrieve the current time.
/// </summary>
public interface IClock
{
    DateTime UtcNow();
}
