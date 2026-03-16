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
        public IEnumerable<Blob> ListBlobs(Regex? regex)
        {
            return blobSystem.ListBlobs()
                .Where(b => regex?.IsMatch(b.Name) ?? true);
        }
    }

    extension(Snapshot snapshot)
    {
        public Blob[] ListBlobs(Regex? regex)
        {
            return snapshot.BlobReferences
                .Select(br => br.Blob)
                .Where(b => regex?.IsMatch(b.Name) ?? true)
                .ToArray();
        }

        public BlobReference[] ListBlobReferences(Regex? regex)
        {
            return snapshot.BlobReferences
                .Where(b => regex?.IsMatch(b.Blob.Name) ?? true)
                .ToArray();
        }
    }

    extension(FlagConsumer consumer)
    {
        public bool TryInt(
            string flag,
            string info,
            out int value,
            int? defaultValue = null)
        {
            return consumer.TryValue(
                flag,
                info,
                out value,
                int.Parse,
                i => i.ToString(),
                defaultValue);
        }

        public bool TryEnum<T>(
            string flag,
            string info,
            out T value,
            T? defaultValue = null)
            where T : struct
        {
            var names = string.Join(", ", Enum.GetNames(typeof(T)));

            return consumer.TryValue<T>(
                flag,
                $"{info}: {names}",
                out value,
                s => Enum.Parse<T>(s, true),
                e => Enum.GetName(typeof(T), e)!,
                defaultValue);
        }

        public bool TrySnapshotStore(out SnapshotStore snapshotStore)
        {
            var success = consumer.TryRepository("--repository", "The repository path", out var repository)
                & consumer.TryDryRun(new ConsoleCryptoFactory(), c => new DryRunCryptoFactory(c), out ICryptoFactory cryptoFactory);

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
                -1);
        }

        public bool TrySnapshots(out int[] snapshotIds)
        {
            return consumer.TryValues(
                "--snapshot",
                "The snapshot IDs",
                out snapshotIds,
                int.Parse,
                i => i.ToString(),
                Array.Empty<int>());
        }

        public bool TryInclude(out Regex include)
        {
            var success = consumer.TryString(
                "--include",
                "A regular expression for files to include",
                out var pattern,
                ".*");

            include = new Regex(pattern);

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
