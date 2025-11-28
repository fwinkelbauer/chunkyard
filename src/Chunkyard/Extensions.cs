namespace Chunkyard;

/// <summary>
/// A collection of extension methods.
/// </summary>
internal static class Extensions
{
    extension<T>(IEnumerable<T> elements)
    {
        public bool CheckAll(Func<T, bool> checkFunc)
        {
            var results = elements.Select(e =>
            {
                try
                {
                    return checkFunc(e);
                }
                catch (Exception)
                {
                    return false;
                }
            });

            return results.Aggregate(true, (total, next) => total && next);
        }
    }

    extension<T>(IRepository<T> repository)
    {
        public byte[] Retrieve(T key)
        {
            using var memoryStream = new MemoryStream();
            using var valueStream = repository.OpenRead(key);

            valueStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }
    }

    extension(IBlobSystem blobSystem)
    {
        public IEnumerable<Blob> ListBlobs(Fuzzy fuzzy)
        {
            return blobSystem.ListBlobs()
                .Where(b => fuzzy.IsMatch(b.Name));
        }
    }

    extension(Snapshot snapshot)
    {
        public Blob[] ListBlobs(Fuzzy fuzzy)
        {
            return snapshot.BlobReferences
                .Select(br => br.Blob)
                .Where(b => fuzzy.IsMatch(b.Name))
                .ToArray();
        }

        public BlobReference[] ListBlobReferences(Fuzzy fuzzy)
        {
            return snapshot.BlobReferences
                .Where(b => fuzzy.IsMatch(b.Blob.Name))
                .ToArray();
        }
    }

    extension(FlagConsumer consumer)
    {
        public bool TrySnapshotStore(out SnapshotStore snapshotStore)
        {
            var success = consumer.TryRepository("--repository", "The repository path", out var repository)
                & consumer.TryEnum("--password", "The password prompt method", out Password password, Password.Console);

            ICryptoFactory cryptoFactory = password switch
            {
                Password.Console => new ConsoleCryptoFactory(),
                Password.Libsecret => new LibsecretCryptoFactory(new ConsoleCryptoFactory()),
                _ => new ConsoleCryptoFactory()
            };

            _ = consumer.TryDryRun(cryptoFactory, c => new DryRunCryptoFactory(c), out cryptoFactory);

            snapshotStore = new SnapshotStore(
                repository,
                new ConsoleProbe(),
                cryptoFactory);

            return success;
        }

        public bool TryRepository(
            string flag,
            string info,
            out IRepository repository)
        {
            var success = consumer.TryString(flag, info, out var path)
                & consumer.TryDryRun(new FileRepository(path), r => new DryRunRepository(r), out repository);

            return success;
        }

        public bool TryBlobSystem(
            string flag,
            string info,
            out IBlobSystem blobSystem)
        {
            var success = consumer.TryStrings(flag, info, out var directories)
                & consumer.TryDryRun(new FileBlobSystem(directories), b => new DryRunBlobSystem(b), out blobSystem);

            return success;
        }

        public bool TrySnapshot(out int snapshot)
        {
            return consumer.TryInt(
                "--snapshot",
                "The snapshot ID",
                out snapshot,
                SnapshotStore.LatestSnapshotId);
        }

        public bool TryInclude(out Fuzzy include)
        {
            var success = consumer.TryStrings(
                "--include",
                "A list of fuzzy patterns for files to include",
                out var includePatterns,
                Array.Empty<string>());

            include = success
                ? new Fuzzy(includePatterns)
                : new Fuzzy();

            return success;
        }

        public bool TryDryRun<T>(
            T input,
            Func<T, T> decorator,
            out T output)
        {
            var success = consumer.TryBool(
                "--dry-run",
                "Do not persist any data changes",
                out var dryRun);

            output = dryRun ? decorator(input) : input;

            return success;
        }
    }
}

/// <summary>
/// Describes password retrieval mechanisms.
/// </summary>
internal enum Password
{
    Console = 0,
    Libsecret = 1
}
