namespace Chunkyard.Core;

/// <summary>
/// A class which uses a <see cref="Repository"/> to store snapshots of a set of
/// blobs.
/// </summary>
public sealed class SnapshotStore
{
    private readonly Repository _repository;
    private readonly FastCdc _fastCdc;
    private readonly IProbe _probe;
    private readonly IClock _clock;
    private readonly Lazy<Crypto> _crypto;
    private readonly Lazy<uint[]> _table;

    public SnapshotStore(
        Repository repository,
        FastCdc fastCdc,
        IProbe probe,
        IClock clock,
        IPrompt prompt)
    {
        _repository = repository;
        _fastCdc = fastCdc;
        _probe = probe;
        _clock = clock;

        _crypto = new Lazy<Crypto>(() =>
        {
            if (_repository.CurrentReferenceId == null)
            {
                return new Crypto(
                    prompt.NewPassword(),
                    Crypto.GenerateSalt(),
                    Crypto.DefaultIterations);
            }
            else
            {
                var snapshotReference = GetSnapshotReference(
                    _repository.CurrentReferenceId.Value);

                return new Crypto(
                    prompt.ExistingPassword(),
                    snapshotReference.Salt,
                    snapshotReference.Iterations);
            }
        });

        _table = new Lazy<uint[]>(
            () => FastCdc.GenerateGearTable(_crypto.Value));
    }

    public DiffSet StoreSnapshotPreview(IBlobSystem blobSystem)
    {
        var blobReferences = _repository.CurrentReferenceId == null
            ? Array.Empty<BlobReference>()
            : GetSnapshot(_repository.CurrentReferenceId.Value).BlobReferences;

        var blobs = blobSystem.ListBlobs();

        return DiffSet.Create(
            blobReferences.Select(br => br.Blob),
            blobs,
            blob => blob.Name);
    }

    public int StoreSnapshot(IBlobSystem blobSystem)
    {
        var snapshot = new Snapshot(
            _clock.NowUtc(),
            StoreBlobs(blobSystem));

        var snapshotId = StoreSnapshotReference(
            new SnapshotReference(
                _crypto.Value.Salt,
                _crypto.Value.Iterations,
                StoreSnapshot(snapshot)));

        _probe.StoredSnapshot(snapshotId);

        return snapshotId;
    }

    public Snapshot GetSnapshot(int snapshotId)
    {
        return GetSnapshot(
            GetSnapshotReference(snapshotId).ChunkIds);
    }

    public bool CheckSnapshotExists(int snapshotId, Fuzzy fuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            fuzzy,
            _repository.ChunkExists);
    }

    public bool CheckSnapshotValid(int snapshotId, Fuzzy fuzzy)
    {
        return CheckSnapshot(
            snapshotId,
            fuzzy,
            _repository.ChunkValid);
    }

    public IReadOnlyCollection<BlobReference> FilterSnapshot(
        int snapshotId,
        Fuzzy fuzzy)
    {
        return GetSnapshot(snapshotId).BlobReferences
            .Where(br => fuzzy.IsMatch(br.Blob.Name))
            .ToArray();
    }

    public DiffSet RestoreSnapshotPreview(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy fuzzy)
    {
        var blobs = blobSystem.ListBlobs();
        var blobReferences = FilterSnapshot(snapshotId, fuzzy);

        var diffSet = DiffSet.Create(
            blobs,
            blobReferences.Select(br => br.Blob),
            blob => blob.Name);

        return new DiffSet(
            diffSet.Added,
            diffSet.Changed,
            Array.Empty<string>());
    }

    public void RestoreSnapshot(
        IBlobSystem blobSystem,
        int snapshotId,
        Fuzzy fuzzy)
    {
        _ = FilterSnapshot(snapshotId, fuzzy)
            .AsParallel()
            .Select(br => RestoreBlob(blobSystem, br))
            .ToArray();

        _probe.RestoredSnapshot(
            _repository.ResolveReferenceId(snapshotId));
    }

    public IReadOnlyCollection<int> ListSnapshotIds()
    {
        return _repository.ListReferenceIds();
    }

    public void GarbageCollect()
    {
        var usedChunkIds = ListChunkIds(_repository.ListReferenceIds());
        var unusedChunkIds = _repository.ListChunkIds()
            .Except(usedChunkIds);

        foreach (var chunkId in unusedChunkIds)
        {
            _repository.RemoveChunk(chunkId);
            _probe.RemovedChunk(chunkId);
        }
    }

    public void RemoveSnapshot(int snapshotId)
    {
        var resolvedSnapshotId = _repository.ResolveReferenceId(snapshotId);

        _repository.RemoveReference(resolvedSnapshotId);
        _probe.RemovedSnapshot(resolvedSnapshotId);
    }

    public void KeepSnapshots(int latestCount)
    {
        var snapshotIds = _repository.ListReferenceIds();
        var snapshotIdsToKeep = snapshotIds.TakeLast(latestCount);
        var snapshotIdsToRemove = snapshotIds.Except(snapshotIdsToKeep)
            .ToArray();

        foreach (var snapshotId in snapshotIdsToRemove)
        {
            RemoveSnapshot(snapshotId);
        }
    }

    public void RestoreChunks(
        IEnumerable<string> chunkIds,
        Stream outputStream)
    {
        foreach (var chunkId in chunkIds)
        {
            try
            {
                var decrypted = _crypto.Value.Decrypt(
                    _repository.RetrieveChunk(chunkId));

                outputStream.Write(decrypted);
            }
            catch (CryptographicException e)
            {
                throw new ChunkyardException(
                    $"Could not decrypt chunk: {chunkId}",
                    e);
            }
        }
    }

    public void CopyTo(Repository otherRepository)
    {
        var snapshotIds = _repository.ListReferenceIds();
        var otherSnapshotIds = otherRepository.ListReferenceIds();
        var otherCurrentSnapshotId = otherRepository.CurrentReferenceId;

        var sharedSnapshotId = snapshotIds.Intersect(otherSnapshotIds)
            .Select(id => id as int?)
            .Max();

        if (sharedSnapshotId != null)
        {
            var bytes = _repository.RetrieveReference(sharedSnapshotId.Value);
            var otherBytes = otherRepository.RetrieveReference(
                sharedSnapshotId.Value);

            if (!bytes.SequenceEqual(otherBytes))
            {
                throw new ChunkyardException(
                    $"Snapshot reference differs: #{sharedSnapshotId}");
            }
        }

        var snapshotIdsToCopy = otherCurrentSnapshotId == null
            ? snapshotIds
            : snapshotIds.Where(id => id > otherCurrentSnapshotId)
                .ToArray();

        var chunkIdsToCopy = ListChunkIds(snapshotIdsToCopy)
            .Except(otherRepository.ListChunkIds())
            .ToArray();

        foreach (var chunkId in chunkIdsToCopy)
        {
            var bytes = _repository.RetrieveChunk(chunkId);

            if (!ChunkId.Valid(chunkId, bytes))
            {
                throw new ChunkyardException(
                    $"Invalid chunk: {chunkId}");
            }

            otherRepository.StoreChunkUnchecked(chunkId, bytes);
            _probe.CopiedChunk(chunkId);
        }

        foreach (var snapshotId in snapshotIdsToCopy)
        {
            var bytes = _repository.RetrieveReference(snapshotId);

            otherRepository.StoreReference(snapshotId, bytes);
            _probe.CopiedSnapshot(snapshotId);
        }
    }

    private Snapshot GetSnapshot(IReadOnlyCollection<string> chunkIds)
    {
        using var memoryStream = new MemoryStream();

        RestoreChunks(chunkIds, memoryStream);

        return Serialize.BytesToSnapshot(memoryStream.ToArray());
    }

    private BlobReference[] StoreBlobs(IBlobSystem blobSystem)
    {
        var currentBlobReferences = _repository.CurrentReferenceId == null
            ? new Dictionary<string, BlobReference>()
            : GetSnapshot(_repository.CurrentReferenceId.Value).BlobReferences
                .ToDictionary(br => br.Blob.Name, br => br);

        BlobReference StoreBlob(Blob blob)
        {
            currentBlobReferences.TryGetValue(blob.Name, out var current);

            if (current != null && current.Blob.Equals(blob))
            {
                return current;
            }

            using var stream = blobSystem.OpenRead(blob.Name);

            var blobReference = new BlobReference(
                blob,
                StoreChunks(stream));

            _probe.StoredBlob(blobReference.Blob.Name);

            return blobReference;
        }

        return blobSystem.ListBlobs()
            .AsParallel()
            .AsOrdered()
            .Select(StoreBlob)
            .ToArray();
    }

    private IReadOnlyCollection<string> StoreSnapshot(Snapshot snapshot)
    {
        using var memoryStream = new MemoryStream(
            Serialize.SnapshotToBytes(snapshot));

        return StoreChunks(memoryStream);
    }

    private int StoreSnapshotReference(SnapshotReference snapshotReference)
    {
        return _repository.AppendReference(
            Serialize.SnapshotReferenceToBytes(snapshotReference));
    }

    private IReadOnlyCollection<string> StoreChunks(Stream stream)
    {
        string StoreChunk(byte[] chunk)
        {
            var encrypted = _crypto.Value.Encrypt(
                Crypto.GenerateNonce(),
                chunk);

            return _repository.StoreChunk(encrypted);
        }

        return _fastCdc.SplitIntoChunks(stream, _table.Value)
            .AsParallel()
            .AsOrdered()
            .Select(StoreChunk)
            .ToArray();
    }

    private bool CheckSnapshot(
        int snapshotId,
        Fuzzy fuzzy,
        Func<string, bool> checkChunkIdFunc)
    {
        var snapshotValid = FilterSnapshot(snapshotId, fuzzy)
            .AsParallel()
            .All(br => CheckBlobReference(br, checkChunkIdFunc));

        _probe.SnapshotValid(
            _repository.ResolveReferenceId(snapshotId),
            snapshotValid);

        return snapshotValid;
    }

    private bool CheckBlobReference(
        BlobReference blobReference,
        Func<string, bool> checkChunkIdFunc)
    {
        var blobValid = blobReference.ChunkIds.All(checkChunkIdFunc);

        _probe.BlobValid(blobReference.Blob.Name, blobValid);

        return blobValid;
    }

    private Blob RestoreBlob(
        IBlobSystem blobSystem,
        BlobReference blobReference)
    {
        var blob = blobReference.Blob;

        if (blobSystem.BlobExists(blob.Name)
            && blobSystem.GetBlob(blob.Name).Equals(blob))
        {
            return blob;
        }

        using (var stream = blobSystem.OpenWrite(blob))
        {
            RestoreChunks(blobReference.ChunkIds, stream);
        }

        _probe.RestoredBlob(blobReference.Blob.Name);

        return blob;
    }

    private IReadOnlyCollection<string> ListChunkIds(
        IEnumerable<int> snapshotIds)
    {
        var chunkIds = new HashSet<string>();

        foreach (var snapshotId in snapshotIds)
        {
            var snapshotChunkIds = GetSnapshotReference(snapshotId).ChunkIds;
            var blobChunkIds = GetSnapshot(snapshotChunkIds).BlobReferences
                .SelectMany(br => br.ChunkIds);

            chunkIds.UnionWith(snapshotChunkIds);
            chunkIds.UnionWith(blobChunkIds);
        }

        return chunkIds;
    }

    private SnapshotReference GetSnapshotReference(int snapshotId)
    {
        try
        {
            return Serialize.BytesToSnapshotReference(
                _repository.RetrieveReference(snapshotId));
        }
        catch (Exception e)
        {
            throw new ChunkyardException(
                $"Could not read snapshot reference: #{snapshotId}",
                e);
        }
    }
}
