namespace Chunkyard.Core;

/// <summary>
/// A class which stores blobs as a <see cref="Snapshot"/>. The possibility to
/// store a snapshot used to be part of the <see cref="SnapshotStore"/> class,
/// but was moved here in order to improve its performance. Previous
/// implementations of "StoreSnapshot" can be found in commits prior to e297430.
/// </summary>
internal class SnapshotWriter
{
    private const int RingBufferLength = 64;

    private readonly IRepository<Uri> _uriRepository;
    private readonly FastCdc _fastCdc;
    private readonly IProbe _probe;
    private readonly AesGcmCrypto _aesGcmCrypto;
    private readonly RingBuffer _unencryptedRingBuffer;
    private readonly RingBuffer _encryptedRingBuffer;
    private readonly ConcurrentDictionary<Uri, object> _locks;

    public SnapshotWriter(
        IRepository<Uri> uriRepository,
        FastCdc fastCdc,
        IProbe probe,
        AesGcmCrypto aesGcmCrypto)
    {
        _uriRepository = uriRepository;
        _fastCdc = fastCdc;
        _probe = probe;
        _aesGcmCrypto = aesGcmCrypto;

        _unencryptedRingBuffer = new RingBuffer(
            RingBufferLength,
            _fastCdc.MaxSize);

        _encryptedRingBuffer = new RingBuffer(
            RingBufferLength,
            _fastCdc.MaxSize + AesGcmCrypto.EncryptionBytes);

        _locks = new ConcurrentDictionary<Uri, object>();
    }

    public IReadOnlyCollection<Uri> WriteObject(object o)
    {
        using var memoryStream = new MemoryStream(
            DataConvert.ObjectToBytes(o));

        return Task.WhenAll(
            WriteStream(
                memoryStream,
                AesGcmCrypto.GenerateNonce()))
            .Result;
    }

    public IReadOnlyCollection<BlobReference> WriteBlobs(
        IBlobSystem blobSystem,
        Fuzzy excludeFuzzy,
        IEnumerable<BlobReference> knownBlobReferences)
    {
        var knownBlobReferencesDict = knownBlobReferences
            .ToDictionary(br => br.Blob.Name, br => br);

        var blobs = blobSystem.ListBlobs(excludeFuzzy);
        var tasks = new List<Task<BlobReference>>();

        foreach (var blob in blobs)
        {
            knownBlobReferencesDict.TryGetValue(
                blob.Name,
                out var knownBlobReference);

            if (knownBlobReference != null
                && knownBlobReference.Blob.Equals(blob))
            {
                tasks.Add(Task.FromResult(knownBlobReference));
                continue;
            }

            // Known blobs should be encrypted using the same nonce
            var nonce = knownBlobReference?.Nonce
                ?? AesGcmCrypto.GenerateNonce();

            var writeBlobTask = WriteBlobAsync(
                blobSystem,
                blob,
                nonce);

            tasks.Add(writeBlobTask);
        }

        return Task.WhenAll(tasks).Result;
    }

    private async Task<BlobReference> WriteBlobAsync(
        IBlobSystem blobSystem,
        Blob blob,
        byte[] nonce)
    {
        using var stream = blobSystem.OpenRead(blob.Name);

        var chunkIds = await Task.WhenAll(WriteStream(stream, nonce))
            .ConfigureAwait(false);

        var blobReference = new BlobReference(
            blob,
            nonce,
            chunkIds);

        _probe.StoredBlob(blobReference.Blob.Name);

        return blobReference;
    }

    private IEnumerable<Task<Uri>> WriteStream(
        Stream stream,
        byte[] nonce)
    {
        long bytesProcessed = 0;
        var bytesCarryOver = 0;
        var chunkSize = 0;

        while (bytesProcessed < stream.Length)
        {
            var ticket = _unencryptedRingBuffer.ReserveTicketBlocking();
            var buffer = _unencryptedRingBuffer.GetWriteBuffer(ticket);

            if (bytesCarryOver > 0)
            {
                Array.Copy(
                    _unencryptedRingBuffer.GetWriteBuffer(ticket - 1),
                    chunkSize,
                    buffer,
                    0,
                    bytesCarryOver);
            }

            var bytesRead = stream.Read(
                buffer,
                bytesCarryOver,
                buffer.Length - bytesCarryOver);

            var bytesTotal = bytesCarryOver + bytesRead;

            chunkSize = _fastCdc.Cut(
                new ReadOnlySpan<byte>(buffer, 0, bytesTotal));

            bytesProcessed += chunkSize;
            bytesCarryOver = bytesTotal - chunkSize;

            _unencryptedRingBuffer.CommitTicketWrite(
                ticket,
                chunkSize);

            yield return Task.Run(() => WriteEncryptedChunk(
                nonce,
                ticket));
        }
    }

    private Uri WriteEncryptedChunk(
        byte[] nonce,
        int readTicket)
    {
        var unencryptedBuffer = _unencryptedRingBuffer.GetReadBuffer(
            readTicket);

        var writeTicket = _encryptedRingBuffer.ReserveTicketBlocking();

        var encryptedBuffer = _encryptedRingBuffer.GetWriteBuffer(
            writeTicket);

        var encryptedSize = _aesGcmCrypto.Encrypt(
            nonce,
            unencryptedBuffer,
            encryptedBuffer);

        _unencryptedRingBuffer.CommitTicketRead(readTicket);

        _encryptedRingBuffer.CommitTicketWrite(
            writeTicket,
            encryptedSize);

        return WriteChunk(_encryptedRingBuffer, writeTicket);
    }

    private Uri WriteChunk(
        RingBuffer ringBuffer,
        int readTicket)
    {
        try
        {
            var readBuffer = ringBuffer.GetReadBuffer(readTicket);
            var chunkId = ChunkId.ComputeChunkId(readBuffer);

            lock (_locks.GetOrAdd(chunkId, _ => new object()))
            {
                if (!_uriRepository.ValueExists(chunkId))
                {
                    _uriRepository.StoreValue(chunkId, readBuffer);
                }
            }

            return chunkId;
        }
        finally
        {
            ringBuffer.CommitTicketRead(readTicket);
        }
    }
}
