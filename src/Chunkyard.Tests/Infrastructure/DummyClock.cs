namespace Chunkyard.Tests.Infrastructure;

internal class DummyClock : IClock
{
    private DateTime _nowUtc;

    public DummyClock(DateTime nowUtc)
    {
        _nowUtc = nowUtc;
    }

    public DateTime NowUtc()
    {
        var temp = _nowUtc;

        _nowUtc = _nowUtc.AddHours(1);

        return temp;
    }
}
