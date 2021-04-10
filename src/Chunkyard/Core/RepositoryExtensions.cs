using System;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IRepository"/>.
    /// </summary>
    public static class RepositoryExtensions
    {
        public static Uri StoreValue(
            this IRepository repository,
            HashAlgorithmName hashAlgorithmName,
            byte[] value)
        {
            repository.EnsureNotNull(nameof(repository));

            var contentUri = Id.ComputeContentUri(
                hashAlgorithmName,
                value);

            repository.StoreValue(contentUri, value);

            return contentUri;
        }

        public static byte[] RetrieveValueValid(
            this IRepository repository,
            Uri contentUri)
        {
            repository.EnsureNotNull(nameof(repository));

            var content = repository.RetrieveValue(contentUri);

            if (!Id.ContentUriValid(contentUri, content))
            {
                throw new ChunkyardException($"Invalid content: {contentUri}");
            }

            return content;
        }

        public static bool ValueValid(
            this IRepository repository,
            Uri contentUri)
        {
            repository.EnsureNotNull(nameof(repository));
            contentUri.EnsureNotNull(nameof(contentUri));

            if (!repository.ValueExists(contentUri))
            {
                return false;
            }

            return Id.ContentUriValid(
                contentUri,
                repository.RetrieveValue(contentUri));
        }

        public static void KeepLatestLogPositions(
            this IRepository repository,
            int count)
        {
            repository.EnsureNotNull(nameof(repository));

            var logPositions = repository.ListLogPositions();
            var logPositionsToKeep = logPositions.TakeLast(count);

            var logPositionsToDelete = logPositions
                .Except(logPositionsToKeep);

            foreach (var logPosition in logPositionsToDelete)
            {
                repository.RemoveFromLog(logPosition);
            }
        }

        public static void Copy(
            this IRepository repository,
            IRepository otherRepository)
        {
            repository.EnsureNotNull(nameof(repository));
            otherRepository.EnsureNotNull(nameof(otherRepository));

            var thisLogs = repository.ListLogPositions();
            var otherLogs = otherRepository.ListLogPositions();
            var intersection = thisLogs.Intersect(otherLogs)
                .ToArray();

            if (intersection.Length == 0
                && otherLogs.Length > 0)
            {
                throw new ChunkyardException(
                    "Cannot operate on repositories without overlapping log positions");
            }

            foreach (var logPosition in intersection)
            {
                var bytes = repository.RetrieveFromLog(logPosition);
                var otherBytes = otherRepository.RetrieveFromLog(logPosition);

                if (!bytes.SequenceEqual(otherBytes))
                {
                    throw new ChunkyardException(
                        $"Repositories differ at position #{logPosition}");
                }
            }

            var otherMax = otherLogs.Length == 0
                ? -1
                : otherLogs.Max();

            var logPositionsToCopy = thisLogs
                .Where(l => l > otherMax)
                .ToArray();

            var urisToCopy = repository.ListUris()
                .Except(otherRepository.ListUris())
                .ToArray();

            foreach (var contentUri in urisToCopy)
            {
                otherRepository.StoreValue(
                    contentUri,
                    repository.RetrieveValue(contentUri));
            }

            foreach (var logPosition in logPositionsToCopy)
            {
                otherRepository.AppendToLog(
                    logPosition,
                    repository.RetrieveFromLog(logPosition));
            }
        }
    }
}
