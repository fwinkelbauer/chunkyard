using System;

namespace Chunkyard
{
    public static class IRepositoryExtensions
    {
        private const string QueryLog = "id";

        public static T RetrieveFromLog<T>(this IRepository repository, Uri uri) where T : IContentRef
        {
            // TODO verify protocol
            var queryValues = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var logText = queryValues.Get(QueryLog);
            var hasValues = repository.TryFetchLogPosition(uri.Host, out var currentLogPosition);

            if (string.IsNullOrEmpty(logText))
            {
                if (!hasValues)
                {
                    throw new ChunkyardException($"{uri} is empty");
                }

                return repository.RetrieveFromLog<T>(uri.Host, currentLogPosition);
            }
            else
            {
                var logPosition = Convert.ToInt32(logText);

                return repository.RetrieveFromLog<T>(uri.Host, logPosition < 0
                    ? currentLogPosition + logPosition
                    : logPosition);
            }
        }

        public static bool AnyLog(this IRepository repository, string logName)
        {
            return repository.TryFetchLogPosition(logName, out var _);
        }

        public static bool ValidContent(this IRepository repository, Uri contentUri, out byte[] content)
        {
            if (!repository.ContentExists(contentUri))
            {
                content = Array.Empty<byte>();
                return false;
            }

            content = repository.RetrieveContent(contentUri);
            var computedUri = Hash.ComputeContentUri(
                Hash.AlgorithmFromContentUri(contentUri),
                content);

            return computedUri.Equals(contentUri);
        }

        public static bool ValidContent(this IRepository repository, Uri contentUri)
        {
            return repository.ValidContent(contentUri, out var _);
        }

        public static void PushContent(this IRepository repository, Uri contentUri, IRepository remoteRepository)
        {
            remoteRepository.StoreContent(
                Hash.AlgorithmFromContentUri(contentUri),
                repository.RetrieveContent(contentUri));
        }
    }
}
