namespace Chunkyard.Core;

/// <summary>
/// A C# port of the Rust crate fastcdc-rc found here:
///
/// https://github.com/nlfiedler/fastcdc-rs
/// https://www.usenix.org/system/files/conference/atc16/atc16-paper-xia.pdf
///
/// The FastCdc algorithm can be used to split data into chunks.
/// </summary>
public sealed class FastCdc
{
    private const int DefaultMin = 4 * 1024 * 1024;
    private const int DefaultAvg = 8 * 1024 * 1024;
    private const int DefaultMax = 16 * 1024 * 1024;

    private const int MinimumMin = 64;
    private const int MinimumMax = 64 * 1024 * 1024;
    private const int AverageMin = 256;
    private const int AverageMax = 256 * 1024 * 1024;
    private const int MaximumMin = 1024;
    private const int MaximumMax = 1024 * 1024 * 1024;

    private readonly uint _maskS;
    private readonly uint _maskL;
    private readonly uint[] _gearTable;

    public FastCdc(
        int minSize,
        int avgSize,
        int maxSize,
        uint[] gearTable)
    {
        MinSize = EnsureBetween(minSize, MinimumMin, MinimumMax);
        AvgSize = EnsureBetween(avgSize, AverageMin, AverageMax);
        MaxSize = EnsureBetween(maxSize, MaximumMin, MaximumMax);

        if (MaxSize - MinSize <= AvgSize)
        {
            throw new ArgumentException(
                $"Invariant violation: {maxSize} - {minSize} > {avgSize}");
        }

        var bits = BitOperations.Log2((uint)AvgSize);
        _maskS = Mask(bits + 1);
        _maskL = Mask(bits - 1);
        _gearTable = gearTable;
    }

    public FastCdc(uint[] gearTable)
        : this(DefaultMin, DefaultAvg, DefaultMax, gearTable)
    {
    }

    public int MinSize { get; }

    public int AvgSize { get; }

    public int MaxSize { get; }

    public ReadOnlySpan<byte> Chunk(Stream stream, Span<byte> buffer)
    {
        var bytesRead = stream.Read(buffer);
        var chunkSize = Cut(buffer[..bytesRead]);
        stream.Position -= (bytesRead - chunkSize);

        return buffer[..chunkSize];
    }

    private int Cut(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length <= MinSize)
        {
            return buffer.Length;
        }

        var center = CenterSize(AvgSize, MinSize, buffer.Length);
        uint hash = 0;
        var offset = MinSize;

        while (offset < center)
        {
            var index = buffer[offset];
            offset++;
            hash = (hash >> 1) + _gearTable[index];

            if ((hash & _maskS) == 0)
            {
                return offset;
            }
        }

        while (offset < buffer.Length)
        {
            var index = buffer[offset];
            offset++;
            hash = (hash >> 1) + _gearTable[index];

            if ((hash & _maskL) == 0)
            {
                return offset;
            }
        }

        return buffer.Length;
    }

    private static int CenterSize(int average, int minimum, int sourceSize)
    {
        var offset = minimum + CeilDiv(minimum, 2);
        var size = average - Math.Min(offset, average);

        return Math.Min(size, sourceSize);
    }

    private static int CeilDiv(int x, int y)
    {
        return 1 + ((x - 1) / y);
    }

    private static uint Mask(int bits)
    {
        _ = EnsureBetween(bits, 1, 31);

        return (1u << bits) - 1;
    }

    private static int EnsureBetween(int value, int min, int max)
    {
        return value < min || value > max
            ? throw new ArgumentOutOfRangeException(
                $"Value must be between {min} and {max}")
            : value;
    }
}
