using System;
using System.Text;
using Chunkyard.Core;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal static class IRepositoryExtensions
    {
        public static T RetrieveFromLog<T>(this IRepository repository, Uri logUri)
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(repository.RetrieveFromLog(logUri)));
        }

        public static T RetrieveFromLog<T>(this IRepository repository, string logName, int logPosition)
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(repository.RetrieveFromLog(logName, logPosition)));
        }
    }
}
