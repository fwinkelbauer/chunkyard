using System;

namespace Chunkyard
{
    public static class IContentRefLogExtensions
    {
        private const string QueryLog = "log";

        public static T Retrieve<T>(this IContentRefLog<T> refLog, Uri uri) where T : IContentRef
        {
            // TODO verify protocol
            var queryValues = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var logText = queryValues.Get(QueryLog);
            var hasValues = refLog.TryFetchLogPosition(uri.Host, out var currentLogPosition);

            if (string.IsNullOrEmpty(logText))
            {
                if (!hasValues)
                {
                    throw new ChunkyardException($"{uri} is empty");
                }

                return refLog.Retrieve(uri.Host, currentLogPosition);
            }
            else
            {
                var logPosition = Convert.ToInt32(logText);

                return refLog.Retrieve(uri.Host, logPosition < 0
                    ? currentLogPosition + logPosition
                    : logPosition);
            }
        }

        public static bool Any<T>(this IContentRefLog<T> refLog, string logName) where T : IContentRef
        {
            return refLog.TryFetchLogPosition(logName, out var _);
        }
    }
}
