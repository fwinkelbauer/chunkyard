namespace Chunkyard.Core;

public class ChunkStore
{
    public const int LatestLogId = -1;
    public const int SecondLatestLogId = -2;

    private readonly IRepository _repository;
    private readonly FastCdc _fastCdc;
    private readonly Lazy<Crypto> _crypto;
    private readonly ConcurrentDictionary<string, object> _locks;

    public ChunkStore(IRepository repository, FastCdc fastCdc, IPrompt prompt)
    {
        _repository = repository;
        _fastCdc = fastCdc;

        _crypto = new Lazy<Crypto>(() =>
        {
            if (CurrentLogId == null)
            {
                return new Crypto(
                    prompt.NewPassword(),
                    Crypto.GenerateSalt(),
                    Crypto.DefaultIterations);
            }
            else
            {
                var logReference = GetLogReference(
                    CurrentLogId.Value);

                return new Crypto(
                    prompt.ExistingPassword(),
                    logReference.Salt,
                    logReference.Iterations);
            }
        });

        _locks = new ConcurrentDictionary<string, object>();

        CurrentLogId = FetchCurrentLogId();
    }

    public int? CurrentLogId { get; private set; }

    public int WriteLog(IReadOnlyCollection<string> chunkIds)
    {
        var logReference = new LogReference(
            _crypto.Value.Salt,
            _crypto.Value.Iterations,
            chunkIds);

        var newLogId = CurrentLogId + 1 ?? 0;

        _repository.Log.StoreValue(
            newLogId,
            Serialize.LogReferenceToBytes(logReference));

        CurrentLogId = newLogId;

        return newLogId;
    }

    public int RemoveLog(int logId)
    {
        var resolvedLogId = ResolveLogId(logId);

        _repository.Log.RemoveValue(resolvedLogId);

        if (CurrentLogId == resolvedLogId)
        {
            CurrentLogId = FetchCurrentLogId();
        }

        return resolvedLogId;
    }

    public int ResolveLogId(int logId)
    {
        //  0: the first element
        //  1: the second element
        // -1: the last element
        // -2: the second-last element
        if (logId >= 0)
        {
            return logId;
        }
        else if (logId == LatestLogId && CurrentLogId != null)
        {
            return CurrentLogId.Value;
        }

        var logIds = _repository.Log.ListKeys().ToArray();
        var position = logIds.Length + logId;

        if (position < 0)
        {
            throw new ChunkyardException(
                $"Could not resolve log reference: #{logId}");
        }

        return logIds[position];
    }

    public void CopyLog(IRepository repository, int logId)
    {
        ArgumentNullException.ThrowIfNull(repository);

        repository.Log.StoreValue(
            logId,
            Serialize.LogReferenceToBytes(
                GetLogReference(logId)));
    }

    public IReadOnlyCollection<int> ListLogIds()
    {
        return _repository.Log.ListKeys();
    }

    public IReadOnlyCollection<string> ListChunkIds(int logId)
    {
        return GetLogReference(logId).ChunkIds;
    }

    public IReadOnlyCollection<string> ListChunkIds()
    {
        return _repository.Chunks.ListKeys();
    }

    public void RemoveChunk(string chunkId)
    {
        _repository.Chunks.RemoveValue(chunkId);
    }

    public IReadOnlyCollection<string> WriteChunks(byte[] nonce, Stream stream)
    {
        string WriteChunk(byte[] chunk)
        {
            var encrypted = _crypto.Value.Encrypt(nonce, chunk);
            var chunkId = ChunkId.Compute(encrypted);

            lock (_locks.GetOrAdd(chunkId, _ => new object()))
            {
                if (!_repository.Chunks.ValueExists(chunkId))
                {
                    _repository.Chunks.StoreValue(chunkId, encrypted);
                }
            }

            return chunkId;
        }

        return _fastCdc.SplitIntoChunks(stream)
            .AsParallel()
            .AsOrdered()
            .Select(WriteChunk)
            .ToArray();
    }

    public void CopyChunk(IRepository repository, string chunkId)
    {
        ArgumentNullException.ThrowIfNull(repository);

        var chunk = GetChunk(chunkId);

        if (!ChunkId.Valid(chunkId, chunk))
        {
            throw new ChunkyardException(
                $"Invalid chunk: {chunkId}");
        }

        repository.Chunks.StoreValue(chunkId, chunk);
    }

    public bool ChunkExists(string chunkId)
    {
        return _repository.Chunks.ValueExists(chunkId);
    }

    public bool ChunkValid(string chunkId)
    {
        return _repository.Chunks.ValueExists(chunkId)
            && ChunkId.Valid(chunkId, GetChunk(chunkId));
    }

    public void RestoreChunks(
        IEnumerable<string> chunkIds,
        Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(chunkIds);
        ArgumentNullException.ThrowIfNull(outputStream);

        foreach (var chunkId in chunkIds)
        {
            var encrypted = GetChunk(chunkId);
            byte[]? decrypted = null;

            try
            {
                decrypted = _crypto.Value.Decrypt(encrypted);
            }
            catch (Exception e)
            {
                throw new ChunkyardException(
                    $"Could not decrypt chunk: {chunkId}",
                    e);
            }

            outputStream.Write(decrypted);
        }
    }

    private LogReference GetLogReference(int logId)
    {
        var resolvedLogId = ResolveLogId(logId);

        try
        {
            return Serialize.BytesToLogReference(
                _repository.Log.RetrieveValue(resolvedLogId));
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read log reference: #{resolvedLogId}",
                e);
        }
    }

    private byte[] GetChunk(string chunkId)
    {
        try
        {
            return _repository.Chunks.RetrieveValue(chunkId);
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read chunk: {chunkId}",
                e);
        }
    }

    private int? FetchCurrentLogId()
    {
        return _repository.Log.ListKeys()
            .Select(id => id as int?)
            .Max();
    }
}
