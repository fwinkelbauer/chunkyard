namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyClock : IClock
{
    private DateTime _now;

    public DummyClock()
    {
        _now = DateTime.UtcNow;
    }

    public DateTime UtcNow()
    {
        _now = _now.AddSeconds(1);

        return _now;
    }
}
