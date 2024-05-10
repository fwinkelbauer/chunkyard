namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IWorld"/> using the current time and real
/// random numbers.
/// </summary>
internal sealed class RealWorld : IWorld
{
    public RealWorld(int parallelism)
    {
        Parallelism = parallelism < 1
            ? Environment.ProcessorCount
            : parallelism;

        Iterations = Crypto.DefaultIterations;
    }

    public int Parallelism { get; }

    public int Iterations { get; }

    public byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(Crypto.SaltBytes);
    }

    public byte[] GenerateNonce()
    {
        return RandomNumberGenerator.GetBytes(Crypto.NonceBytes);
    }

    public DateTime NowUtc()
    {
        return DateTime.UtcNow;
    }
}
