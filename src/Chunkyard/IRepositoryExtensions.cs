using System;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IRepository"/>.
    /// </summary>
    public static class IRepositoryExtensions
    {
        public static Uri StoreValue(
            this IRepository repository,
            HashAlgorithmName hashAlgorithmName,
            byte[] value,
            out bool isNewValue)
        {
            repository.EnsureNotNull(nameof(repository));

            var contentUri = Id.ComputeContentUri(
                hashAlgorithmName,
                value);

            isNewValue = repository.StoreValue(contentUri, value);

            return contentUri;
        }

        public static bool UriValid(this IRepository repository, Uri contentUri)
        {
            repository.EnsureNotNull(nameof(repository));
            contentUri.EnsureNotNull(nameof(contentUri));

            if (!repository.ValueExists(contentUri))
            {
                return false;
            }

            var content = repository.RetrieveValue(contentUri);
            var computedUri = Id.ComputeContentUri(
                Id.AlgorithmFromContentUri(contentUri),
                content);

            return contentUri.Equals(computedUri);
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
