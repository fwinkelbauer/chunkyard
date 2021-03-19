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

            var (algorithm, hash) = Id.DeconstructContentUri(contentUri);
            var content = repository.RetrieveValue(contentUri);
            var computedHash = Id.ComputeHash(algorithm, content);

            return hash.Equals(computedHash);
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
    }
}
