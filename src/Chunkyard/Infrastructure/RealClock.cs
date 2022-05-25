namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IClock"/> using the current time.
/// </summary>
internal class RealClock : IClock
{
    public DateTime NowUtc()
    {
        return DateTime.UtcNow;
    }
}
