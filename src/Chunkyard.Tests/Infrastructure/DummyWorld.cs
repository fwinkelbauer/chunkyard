namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyWorld : IWorld
{
    private DateTime _now;

    public DummyWorld()
    {
        _now = DateTime.UtcNow;
    }

    public int Parallelism => 2;

    public int Iterations => 1;

    public byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(Crypto.SaltBytes);
    }

    public DateTime UtcNow()
    {
        _now = _now.AddSeconds(1);

        return _now;
    }
}
