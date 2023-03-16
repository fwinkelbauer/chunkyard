namespace Chunkyard.Core;

/// <summary>
/// An interface to retrieve the current time and random numbers.
/// </summary>
public interface IWorld
{
    DateTime NowUtc();

    byte[] GenerateSalt();

    byte[] GenerateNonce();
}
