namespace Chunkyard.Core;

/// <summary>
/// An abstraction based on <see cref="IRepository"/> which provides a
/// content-addressable storage and a reference log.
/// </summary>
public sealed class Repository
{
    public const int LatestReferenceId = -1;
    public const int SecondLatestReferenceId = -2;

    private readonly IRepository<int> _references;
    private readonly IRepository<string> _chunks;

    public Repository(
        IRepository<int> references,
        IRepository<string> chunks)
    {
        _references = references;
        _chunks = chunks;

        CurrentReferenceId = _references.ListKeys()
            .Select(id => id as int?)
            .Max();
    }

    public int? CurrentReferenceId { get; private set; }

    public int AppendReference(ReadOnlySpan<byte> bytes)
    {
        var referenceId = CurrentReferenceId + 1 ?? 0;

        StoreReference(referenceId, bytes);

        return referenceId;
    }

    public void StoreReference(int referenceId, ReadOnlySpan<byte> bytes)
    {
        _references.StoreValue(referenceId, bytes);

        if (CurrentReferenceId == null
            || CurrentReferenceId < referenceId)
        {
            CurrentReferenceId = referenceId;
        }
    }

    public byte[] RetrieveReference(int referenceId)
    {
        return _references.RetrieveValue(
            ResolveReferenceId(referenceId));
    }

    public void RemoveReference(int referenceId)
    {
        var resolvedReferenceId = ResolveReferenceId(referenceId);

        _references.RemoveValue(resolvedReferenceId);

        if (CurrentReferenceId == resolvedReferenceId)
        {
            CurrentReferenceId = _references.ListKeys()
                .Select(id => id as int?)
                .Max();
        }
    }

    public IReadOnlyCollection<int> ListReferenceIds()
    {
        var referenceIds = _references.ListKeys()
            .ToArray();

        Array.Sort(referenceIds);

        return referenceIds;
    }

    public int ResolveReferenceId(int referenceId)
    {
        //  0: the first element
        //  1: the second element
        // -1: the last element
        // -2: the second-last element
        if (referenceId >= 0)
        {
            return referenceId;
        }
        else if (referenceId == LatestReferenceId && CurrentReferenceId != null)
        {
            return CurrentReferenceId.Value;
        }

        var referenceIds = _references.ListKeys()
            .ToArray();

        Array.Sort(referenceIds);

        var position = referenceIds.Length + referenceId;

        if (position < 0)
        {
            throw new ChunkyardException(
                $"Could not resolve reference: #{referenceId}");
        }

        return referenceIds[position];
    }

    public string StoreChunk(ReadOnlySpan<byte> bytes)
    {
        var chunkId = ChunkId.Compute(bytes);

        StoreChunkUnchecked(chunkId, bytes);

        return chunkId;
    }

    public void StoreChunkUnchecked(string chunkId, ReadOnlySpan<byte> bytes)
    {
        _chunks.StoreValueIfNotExists(chunkId, bytes);
    }

    public byte[] RetrieveChunk(string chunkId)
    {
        return _chunks.RetrieveValue(chunkId);
    }

    public void RemoveChunk(string chunkId)
    {
        _chunks.RemoveValue(chunkId);
    }

    public bool ChunkExists(string chunkId)
    {
        return _chunks.ValueExists(chunkId);
    }

    public bool ChunkValid(string chunkId)
    {
        return ChunkExists(chunkId)
            && ChunkId.Valid(chunkId, RetrieveChunk(chunkId));
    }

    public IReadOnlyCollection<string> ListChunkIds()
    {
        return _chunks.ListKeys();
    }
}
