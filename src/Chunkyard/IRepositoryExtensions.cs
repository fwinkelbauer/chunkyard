using System;
using System.Linq;

namespace Chunkyard
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IRepository"/>.
    /// </summary>
    public static class IRepositoryExtensions
    {
        public static bool UriValid(this IRepository repository, Uri contentUri)
        {
            repository.EnsureNotNull(nameof(repository));

            if (contentUri == null || !repository.ValueExists(contentUri))
            {
                return false;
            }

            var content = repository.RetrieveValue(contentUri);
            var computedUri = Id.ComputeContentUri(
                Id.AlgorithmFromContentUri(contentUri),
                content);

            return contentUri.Equals(computedUri);
        }

        public static int? FetchLogPosition(this IRepository repository)
        {
            repository.EnsureNotNull(nameof(repository));

            var logPositions = repository.ListLogPositions()
                .ToArray();

            if (logPositions.Length == 0)
            {
                return null;
            }

            return logPositions[^1];
        }
    }
}
