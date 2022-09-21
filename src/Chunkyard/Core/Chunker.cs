namespace Chunkyard.Core;

/// <summary>
/// TODO
/// </summary>
internal sealed class Chunker
{
    private readonly Repository _repository;
    private readonly FastCdc _fastCdc;
    private readonly Lazy<Crypto> _crypto;

    public Chunker(Repository repository, FastCdc fastCdc, Lazy<Crypto> crypto)
    {
        _repository = repository;
        _fastCdc = fastCdc;
        _crypto = crypto;
    }

    public IReadOnlyCollection<string> StoreChunks(Stream stream)
    {
        var carryOverBuffer = new byte[_fastCdc.MaxSize];
        var bytesCarryOver = 0;
        long bytesProcessed = 0;

        var tasks = new List<Task<string>>();

        while (bytesProcessed < stream.Length)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(_fastCdc.MaxSize);

            Array.Copy(carryOverBuffer, 0, buffer, 0, bytesCarryOver);

            var bytesRead = stream.Read(
                buffer,
                bytesCarryOver,
                buffer.Length - bytesCarryOver);

            var bytesTotal = bytesCarryOver + bytesRead;

            var chunkSize = _fastCdc.Cut(
                new ReadOnlySpan<byte>(buffer, 0, bytesTotal));

            bytesProcessed += chunkSize;
            bytesCarryOver = bytesTotal - chunkSize;

            Array.Copy(buffer, chunkSize, carryOverBuffer, 0, bytesCarryOver);

            tasks.Add(Task.Run(() => StoreChunk(buffer, chunkSize)));
        }

        return Task.WhenAll(tasks).Result;
    }

    private string StoreChunk(byte[] unencryptedBuffer, int chunkSize)
    {
        var encryptedBuffer = ArrayPool<byte>.Shared.Rent(12 + _fastCdc.MaxSize + 16);
        var encryptedSize = _crypto.Value.Encrypt(
            Crypto.GenerateNonce(),
            new ReadOnlySpan<byte>(unencryptedBuffer, 0, chunkSize),
            encryptedBuffer);

        ArrayPool<byte>.Shared.Return(unencryptedBuffer);

        try
        {
            return _repository.StoreChunk(
                new ReadOnlySpan<byte>(encryptedBuffer, 0, encryptedSize));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(encryptedBuffer);
        }
    }
}
