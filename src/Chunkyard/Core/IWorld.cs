namespace Chunkyard.Core;

/// <summary>
/// An interface to retrieve the current time and crypto parameters.
/// </summary>
public interface IWorld
{
    int Parallelism { get; }

    int Iterations { get; }

    byte[] GenerateSalt();

    DateTime UtcNow();
}
