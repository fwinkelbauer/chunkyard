namespace Chunkyard.Core;

/// <summary>
/// A C# port of the Rust crate fastcdc-rc found here:
///
/// https://github.com/nlfiedler/fastcdc-rs
/// https://www.usenix.org/system/files/conference/atc16/atc16-paper-xia.pdf
///
/// The FastCdc algorithm can be used to split data into chunks.
/// </summary>
public sealed class FastChunker
{
    private const int MinimumMin = 64;
    private const int MinimumMax = 64 * 1024 * 1024;
    private const int AverageMin = 256;
    private const int AverageMax = 256 * 1024 * 1024;
    private const int MaximumMin = 1024;
    private const int MaximumMax = 1024 * 1024 * 1024;

    private readonly int _minSize;
    private readonly int _avgSize;
    private readonly int _maxSize;
    private readonly uint _maskS;
    private readonly uint _maskL;
    private readonly uint[] _gearTable;

    public FastChunker(
        int minSize,
        int avgSize,
        int maxSize,
        uint[] gearTable)
    {
        _minSize = EnsureBetween(minSize, MinimumMin, MinimumMax);
        _avgSize = EnsureBetween(avgSize, AverageMin, AverageMax);
        _maxSize = EnsureBetween(maxSize, MaximumMin, MaximumMax);

        if (_maxSize - _minSize <= _avgSize)
        {
            throw new ArgumentException(
                $"Invariant violation: {maxSize} - {minSize} > {avgSize}");
        }

        var bits = BitOperations.Log2((uint)_avgSize);
        _maskS = Mask(bits + 1);
        _maskL = Mask(bits - 1);
        _gearTable = gearTable;
    }

    public FastChunker(
        int minSize,
        int avgSize,
        int maxSize,
        Crypto crypto)
        : this(minSize, avgSize, maxSize, GenerateGearTable(crypto))
    {
    }

    public ReadOnlySpan<byte> Chunk(Stream stream, Span<byte> buffer)
    {
        var bytesRead = stream.Read(buffer);
        var chunkSize = Cut(buffer[..bytesRead]);
        stream.Position -= (bytesRead - chunkSize);

        return buffer[..chunkSize];
    }

    private int Cut(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length <= _minSize)
        {
            return buffer.Length;
        }

        var center = CenterSize(_avgSize, _minSize, buffer.Length);
        uint hash = 0;
        var offset = _minSize;

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

    // We encrypt an array of zeros using a given key to create reproducible
    // "random" data. This means that the same cryptographic key will always
    // produce the same output, while another key will produce a different
    // output.
    private static uint[] GenerateGearTable(Crypto crypto)
    {
        var input = new byte[1024];

        Crypto.Deconstruct(
            crypto.Encrypt(input),
            out _,
            out var random,
            out _);

        var gearTable = new uint[256];
        var mask = Mask(31);

        for (var i = 0; i < gearTable.Length; i++)
        {
            var slice = random.Slice(i * 4, 4);
            gearTable[i] = BitConverter.ToUInt32(slice) & mask;
        }

        return gearTable;
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
