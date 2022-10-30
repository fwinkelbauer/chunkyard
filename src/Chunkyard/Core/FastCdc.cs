namespace Chunkyard.Core;

/// <summary>
/// A C# port of the Rust crate fastcdc-rc found here:
///
/// https://github.com/nlfiedler/fastcdc-rs
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

    public FastCdc(
        int minSize,
        int avgSize,
        int maxSize)
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

        if ((MaxSize - MinSize) <= AvgSize)
        {
            throw new ArgumentException(
                "Invariant violation: maxSize - minSize > avgSize");
        }

        var bits = Logarithm2(AvgSize);
        _maskS = Mask(bits + 1);
        _maskL = Mask(bits - 1);
    }

    public FastCdc()
        : this(DefaultMin, DefaultAvg, DefaultMax)
    {
    }

    public int MinSize { get; }

    public int AvgSize { get; }

    public int MaxSize { get; }

    public static uint[] GenerateGearTable(Crypto crypto)
    {
        ArgumentNullException.ThrowIfNull(crypto);

        var nonce = new byte[Crypto.NonceBytes];
        var input = new byte[1024];

        var random = new Span<byte>(
            crypto.Encrypt(nonce, input),
            Crypto.NonceBytes,
            input.Length);

        var table = new uint[256];
        var mask = Mask(31);

        for (var i = 0; i < table.Length; i++)
        {
            var slice = random.Slice(i * 4, 4);
            table[i] = BitConverter.ToUInt32(slice) & mask;
        }

        return table;
    }

    public IEnumerable<byte[]> SplitIntoChunks(Stream stream, uint[] table)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(table);

        var buffer = new byte[MaxSize];
        var bytesCarryOver = 0;
        long bytesProcessed = 0;

        while (bytesProcessed < stream.Length)
        {
            var bytesRead = stream.Read(
                buffer,
                bytesCarryOver,
                buffer.Length - bytesCarryOver);

            var bytesTotal = bytesCarryOver + bytesRead;

            var chunkSize = Cut(
                new ReadOnlySpan<byte>(buffer, 0, bytesTotal),
                table);

            yield return new Span<byte>(buffer, 0, chunkSize)
                .ToArray();

            bytesProcessed += chunkSize;
            bytesCarryOver = bytesTotal - chunkSize;

            Array.Copy(buffer, chunkSize, buffer, 0, bytesCarryOver);
        }
    }

    private int Cut(ReadOnlySpan<byte> buffer, uint[] table)
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
            hash = (hash >> 1) + table[index];

            if ((hash & _maskS) == 0)
            {
                return offset;
            }
        }

        while (offset < buffer.Length)
        {
            var index = buffer[offset];
            offset++;
            hash = (hash >> 1) + table[index];

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
        EnsureBetween(bits, 1, 31, nameof(bits));

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
