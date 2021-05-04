using System;

namespace Chunkyard.Core
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IRepository{T}"/>.
    /// </summary>
    public static class RepositoryExtensions
    {
        public static Uri StoreValue(
            this IRepository<Uri> repository,
            string hashAlgorithmName,
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
            this IRepository<Uri> repository,
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
            this IRepository<Uri> repository,
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
    }
}
