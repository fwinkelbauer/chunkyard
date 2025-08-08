namespace Chunkyard.Core;

/// <summary>
/// A C# port of the Rust crate fastcdc-rc found here:
///
/// https://github.com/nlfiedler/fastcdc-rs
/// https://www.usenix.org/system/files/conference/atc16/atc16-paper-xia.pdf
///
/// The FastCdc algorithm can be used to split data into chunks.
/// </summary>
public sealed class FastChunker : IDisposable
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
    private readonly Stream _stream;

    public FastChunker(
        int minSize,
        int avgSize,
        int maxSize,
        uint[] gearTable,
        Stream stream)
    {
        MinSize = EnsureBetween(
            minSize,
            MinimumMin,
            MinimumMax,
            nameof(minSize));

        AvgSize = EnsureBetween(
            avgSize,
            AverageMin,
            AverageMax,
            nameof(avgSize));

        MaxSize = EnsureBetween(
            maxSize,
            MaximumMin,
            MaximumMax,
            nameof(maxSize));

        if (MaxSize - MinSize <= AvgSize)
        {
            throw new ArgumentException(
                $"Invariant violation: {nameof(maxSize)} - {nameof(minSize)} > {nameof(avgSize)}");
        }

        var bits = Logarithm2(AvgSize);
        _maskS = Mask(bits + 1);
        _maskL = Mask(bits - 1);
        _gearTable = gearTable;
        _stream = new BufferedStream(stream, MaxSize);
    }

    public FastChunker(uint[] gearTable, Stream stream)
        : this(DefaultMin, DefaultAvg, DefaultMax, gearTable, stream)
    {
    }

    public int MinSize { get; }

    public int AvgSize { get; }

    public int MaxSize { get; }

    public void Dispose()
    {
        _stream.Dispose();
    }

    // We encrypt an array of zeros using a given key to create reproducible
    // "random" data. This means that the same cryptographic key will always
    // produce the same output, while another key will produce a different
    // output.
    public static uint[] GenerateGearTable(Crypto crypto)
    {
        var input = new byte[1024];

        var random = crypto.Encrypt(input)
            .AsSpan(Crypto.NonceBytes, input.Length);

        var gearTable = new uint[256];
        var mask = Mask(31);

        for (var i = 0; i < gearTable.Length; i++)
        {
            var slice = random.Slice(i * 4, 4);
            gearTable[i] = BitConverter.ToUInt32(slice) & mask;
        }

        return gearTable;
    }

    public int Chunk(Span<byte> buffer)
    {
        var bytesRead = _stream.Read(buffer);
        var chunkSize = Cut(buffer[..bytesRead]);
        _stream.Position -= (bytesRead - chunkSize);

        return chunkSize;
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

    private static int Logarithm2(int value)
    {
        return (int)Math.Round(Math.Log(value, 2));
    }

    private static uint Mask(int bits)
    {
        _ = EnsureBetween(bits, 1, 31, nameof(bits));

        return (uint)Math.Pow(2, bits) - 1;
    }

    private static int EnsureBetween(
        int value,
        int min,
        int max,
        string paramName)
    {
        return value < min || value > max
            ? throw new ArgumentOutOfRangeException(
                paramName,
                $"Value must be between {min} and {max}")
            : value;
    }
}
