namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyWorld : IWorld
{
    public int Parallelism => 2;

    public int Iterations => 1;

    public byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(Crypto.SaltBytes);
    }

    public byte[] GenerateNonce()
    {
        return RandomNumberGenerator.GetBytes(Crypto.NonceBytes);
    }

    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}
