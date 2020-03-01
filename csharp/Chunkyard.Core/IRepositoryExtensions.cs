using System;

namespace Chunkyard.Core
{
    public static class IRepositoryExtensions
    {
        public static T RetrieveFromLog<T>(this IRepository repository, Uri logUri) where T : IContentRef
        {
            var (logName, logPositionCandidate) = Id.LogUriToParts(logUri);
            var currentLogPosition = repository.FetchLogPosition(logName);

            if (!currentLogPosition.HasValue)
            {
                throw new ChunkyardException($"{logUri} is empty");
            }
            else if (logPositionCandidate.HasValue)
            {
                return repository.RetrieveFromLog<T>(logUri.Host, logPositionCandidate.Value < 0
                    ? currentLogPosition.Value + logPositionCandidate.Value
                    : logPositionCandidate.Value);
            }
            else
            {
                return repository.RetrieveFromLog<T>(logUri.Host, currentLogPosition.Value);
            }
        }

        public static bool AnyLog(this IRepository repository, string logName)
        {
            return repository.FetchLogPosition(logName).HasValue;
        }

        public static void PushContent(this IRepository repository, Uri contentUri, IRepository remoteRepository)
        {
            remoteRepository.StoreContent(
                Id.AlgorithmFromContentUri(contentUri),
                repository.RetrieveContent(contentUri));
        }
    }
}
