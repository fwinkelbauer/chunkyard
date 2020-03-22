using System;

namespace Chunkyard
{
    internal static class IRepositoryExtensions
    {
        public static bool Valid(this IRepository repository, Uri contentUri)
        {
            if (!repository.ContentExists(contentUri))
            {
                return false;
            }

            var content = repository.RetrieveContent(contentUri);
            var computedUri = Id.ComputeContentUri(
                Id.AlgorithmFromContentUri(contentUri),
                content);

            return contentUri.Equals(computedUri);
        }

        public static void ThrowIfNotExists(this IRepository repository, Uri contentUri)
        {
            if (!repository.ContentExists(contentUri))
            {
                throw new ChunkyardException($"Missing content: {contentUri}");
            }
        }

        public static void ThrowIfInvalid(this IRepository repository, Uri contentUri)
        {
            if (!repository.Valid(contentUri))
            {
                throw new ChunkyardException($"Corrupted content: {contentUri}");
            }
        }

        public static byte[] RetrieveContentChecked(this IRepository repository, Uri contentUri)
        {
            var content = repository.RetrieveContent(contentUri);
            var computedUri = Id.ComputeContentUri(
                Id.AlgorithmFromContentUri(contentUri),
                content);

            if (!contentUri.Equals(computedUri))
            {
                throw new ChunkyardException($"Corrupted content: {contentUri}");
            }

            return content;
        }

        public static byte[] RetrieveFromLog(this IRepository repository, Uri logUri)
        {
            var (logName, logPositionCandidate) = Id.LogUriToParts(logUri);
            var currentLogPosition = repository.FetchLogPosition(logName);

            if (!currentLogPosition.HasValue)
            {
                throw new ChunkyardException($"{logUri} is empty");
            }
            else if (logPositionCandidate.HasValue)
            {
                //  0: the first element
                //  1: the second element
                // -2: the second-last element
                // -1: the last element
                return repository.RetrieveFromLog(logUri.Host, logPositionCandidate.Value < 0
                    ? currentLogPosition.Value + logPositionCandidate.Value + 1
                    : logPositionCandidate.Value);
            }
            else
            {
                return repository.RetrieveFromLog(logUri.Host, currentLogPosition.Value);
            }
        }

        public static void PushContent(this IRepository repository, Uri contentUri, IRepository remoteRepository)
        {
            remoteRepository.StoreContent(
                contentUri,
                repository.RetrieveContent(contentUri));
        }
    }
}
