namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyWorld : IWorld
{
    public int Parallelism => 2;

    public int Iterations => 1;

    public byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(Crypto.SaltBytes);
    }

    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}
