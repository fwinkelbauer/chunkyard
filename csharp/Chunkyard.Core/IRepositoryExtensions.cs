using System;

namespace Chunkyard.Core
{
    public static class IRepositoryExtensions
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
                return repository.RetrieveFromLog(logUri.Host, logPositionCandidate.Value < 0
                    ? currentLogPosition.Value + logPositionCandidate.Value
                    : logPositionCandidate.Value);
            }
            else
            {
                return repository.RetrieveFromLog(logUri.Host, currentLogPosition.Value);
            }
        }

        public static bool AnyLog(this IRepository repository, string logName)
        {
            return repository.FetchLogPosition(logName).HasValue;
        }

        public static void PushContent(this IRepository repository, Uri contentUri, IRepository remoteRepository)
        {
            if (remoteRepository.ContentExists(contentUri))
            {
                return;
            }

            remoteRepository.StoreContent(
                Id.AlgorithmFromContentUri(contentUri),
                repository.RetrieveContent(contentUri));
        }

        public static void PullContent(this IRepository repository, Uri contentUri, IRepository remoteRepository)
        {
            remoteRepository.PushContent(contentUri, repository);
        }
    }
}
