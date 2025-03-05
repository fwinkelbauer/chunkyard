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
    }

    public int Parallelism { get; }

    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }
}
