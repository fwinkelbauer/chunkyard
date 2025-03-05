namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyWorld : IWorld
{
    private DateTime _now;

    public DummyWorld()
    {
        _now = DateTime.UtcNow;
    }

    public int Parallelism => 2;

    public DateTime UtcNow()
    {
        _now = _now.AddSeconds(1);

        return _now;
    }
}
