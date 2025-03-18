namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IClock"/> using the current time.
/// </summary>
internal sealed class RealClock : IClock
{
    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}
