namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyWorld : IWorld
{
    private DateTime _nowUtc;

    public DummyWorld(DateTime nowUtc)
    {
        _nowUtc = nowUtc;
    }

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

    public DateTime NowUtc()
    {
        var temp = _nowUtc;

        _nowUtc = _nowUtc.AddHours(1);

        return temp;
    }
}
