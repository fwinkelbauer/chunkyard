namespace Chunkyard.Core;

/// <summary>
/// A class to store and retrieve bytes in an encrypted content addressable
/// storage.
/// </summary>
public sealed class Chunker
{
    private readonly IRepository<string> _repository;
    private readonly Crypto _crypto;
    private readonly FastCdc _chunker;
    private readonly byte[] _plainBuffer;
    private readonly byte[] _cipherBuffer;

    public Chunker(IRepository<string> repository, Crypto crypto)
    {
        _repository = repository;
        _crypto = crypto;
        _chunker = new FastCdc(GenerateGearTable(crypto));
        _plainBuffer = new byte[_chunker.MaxSize];
        _cipherBuffer = new byte[_chunker.MaxSize + Crypto.CryptoBytes];
    }

    public string Salt => _crypto.Salt;

    public int Iterations => _crypto.Iterations;

    public string[] StoreChunks(Stream stream)
    {
        var chunkIds = new List<string>();
        ReadOnlySpan<byte> chunk;

        while ((chunk = _chunker.Chunk(stream, _plainBuffer)).Length != 0)
        {
            var cipher = _crypto.Encrypt(chunk, _cipherBuffer);
            var chunkId = ToChunkId(cipher);

            _repository.Write(chunkId, cipher);
            chunkIds.Add(chunkId);
        }

        return chunkIds.ToArray();
    }

    public bool CheckChunk(string chunkId)
    {
        return _repository.Exists(chunkId)
            && chunkId.Equals(ToChunkId(RetrieveChunk(chunkId)));
    }

    public void RestoreChunks(
        IEnumerable<string> chunkIds,
        Stream outputStream)
    {
        foreach (var chunkId in chunkIds)
        {
            var plain = _crypto.Decrypt(RetrieveChunk(chunkId), _plainBuffer);

            outputStream.Write(plain);
        }
    }

    private ReadOnlySpan<byte> RetrieveChunk(string chunkId)
    {
        using var stream = _repository.OpenRead(chunkId);

        var bytesRead = stream.Read(_cipherBuffer);

        return _cipherBuffer.AsSpan(0, bytesRead);
    }

    // We encrypt an array of zeros using a given key to create reproducible
    // "random" data. This means that the same cryptographic key will always
    // produce the same output, while another key will produce a different
    // output.
    private static uint[] GenerateGearTable(Crypto crypto)
    {
        var input = new byte[1024 - Crypto.CryptoBytes];
        var random = crypto.Encrypt(input);
        var gearTable = new uint[256];

        for (var i = 0; i < gearTable.Length; i++)
        {
            var slice = random.AsSpan(i * 4, 4);
            gearTable[i] = BitConverter.ToUInt32(slice) & 0x7FFFFFFF;
        }

        return gearTable;
    }

    private static string ToChunkId(ReadOnlySpan<byte> chunk)
    {
        return Convert.ToHexString(SHA256.HashData(chunk))
            .ToLowerInvariant();
    }
}
