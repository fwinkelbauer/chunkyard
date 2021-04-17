using System;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IRepository{T}"/>.
    /// </summary>
    public static class RepositoryExtensions
    {
        public static Uri StoreValue(
            this IRepository<Uri> repository,
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

        public static void KeepLatestValues(
            this IRepository<int> repository,
            int count)
        {
            repository.EnsureNotNull(nameof(repository));

            var keys = repository.ListKeys();

            Array.Sort(keys);

            var keysToKeep = keys.TakeLast(count);
            var keysToDelete = keys.Except(keysToKeep);

            foreach (var key in keysToDelete)
            {
                repository.RemoveValue(key);
            }
        }

        public static void Copy<T>(
            this IRepository<T> repository,
            IRepository<T> otherRepository)
        {
            repository.EnsureNotNull(nameof(repository));
            otherRepository.EnsureNotNull(nameof(otherRepository));

            var keysToCopy = repository.ListKeys()
                .Except(otherRepository.ListKeys())
                .ToArray();

            foreach (var key in keysToCopy)
            {
                otherRepository.StoreValue(
                    key,
                    repository.RetrieveValue(key));
            }
        }
    }
}
