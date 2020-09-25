using System;
using System.IO;
using Newtonsoft.Json;

namespace Chunkyard
{
    /// <summary>
    /// A decorator of <see cref="IContentStore"/> which remembers stored files
    /// in a separate cache. This cache can be used to skip store operations if
    /// the files (based on their meta data) have not changed.
    /// </summary>
    internal class CachedContentStore : IContentStore
    {
        private readonly IContentStore _contentStore;
        private readonly string _cacheDirectory;

        public CachedContentStore(
            IContentStore contentStore,
            string cacheDirectory)
        {
            _contentStore = contentStore;
            _cacheDirectory = cacheDirectory;
        }

        public int? CurrentLogPosition => _contentStore.CurrentLogPosition;

        public void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            _contentStore.RetrieveContent(contentReference, outputStream);
        }

        public ContentReference StoreContent(
            Stream inputStream,
            string contentName)
        {
            if (!(inputStream is FileStream fileStream))
            {
                return _contentStore.StoreContent(
                    inputStream,
                    contentName);
            }

            var storedReference = RetrieveFromCache(fileStream, contentName);

            if (storedReference != null)
            {
                return storedReference;
            }

            var contentReference = _contentStore.StoreContent(
                inputStream,
                contentName);

            StoreInCache(
                fileStream,
                contentReference);

            return contentReference;
        }

        public ContentReference StoreContent(
            Stream inputStream,
            ContentReference previousContentReference)
        {
            if (!(inputStream is FileStream fileStream))
            {
                return _contentStore.StoreContent(
                    inputStream,
                    previousContentReference);
            }

            var storedReference = RetrieveFromCache(
                fileStream,
                previousContentReference.Name);

            if (storedReference != null)
            {
                return storedReference;
            }

            var contentReference = _contentStore.StoreContent(
                inputStream,
                previousContentReference);

            StoreInCache(
                fileStream,
                contentReference);

            return contentReference;
        }

        public bool ContentExists(ContentReference contentReference)
        {
            return _contentStore.ContentExists(contentReference);
        }

        public bool ContentValid(ContentReference contentReference)
        {
            return _contentStore.ContentValid(contentReference);
        }

        public int AppendToLog(
            Guid logId,
            ContentReference contentReference,
            int newLogPosition)
        {
            return _contentStore.AppendToLog(
                logId,
                contentReference,
                newLogPosition);
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return _contentStore.RetrieveFromLog(logPosition);
        }

        private ContentReference? RetrieveFromCache(
            FileStream fileStream,
            string contentName)
        {
            var cacheFile = ToCacheFile(contentName);

            if (!File.Exists(cacheFile))
            {
                return null;
            }

            var storedCache = JsonConvert.DeserializeObject<Cache>(
                File.ReadAllText(cacheFile));

            var creationDateUtc = File.GetCreationTimeUtc(fileStream.Name);
            var lastWriteDateUtc = File.GetLastWriteTimeUtc(fileStream.Name);

            if (storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(creationDateUtc)
                && storedCache.LastWriteDateUtc.Equals(lastWriteDateUtc)
                && _contentStore.ContentExists(storedCache.ContentReference))
            {
                return storedCache.ContentReference;
            }
            else
            {
                return null;
            }
        }

        private void StoreInCache(
            FileStream fileStream,
            ContentReference contentReference)
        {
            var cacheFile = ToCacheFile(contentReference.Name);

            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile));

            var cache = new Cache(
                contentReference,
                fileStream.Length,
                File.GetCreationTimeUtc(fileStream.Name),
                File.GetLastWriteTimeUtc(fileStream.Name));

            File.WriteAllText(
                cacheFile,
                JsonConvert.SerializeObject(cache));
        }

        private string ToCacheFile(string contentName)
        {
            return Path.Combine(_cacheDirectory, $"{contentName}.json");
        }

        private class Cache
        {
            public Cache(
                ContentReference contentReference,
                long length,
                DateTime creationDateUtc,
                DateTime lastWriteDateUtc)
            {
                ContentReference = contentReference;
                Length = length;
                CreationDateUtc = creationDateUtc;
                LastWriteDateUtc = lastWriteDateUtc;
            }

            public ContentReference ContentReference { get; }

            public long Length { get; }

            public DateTime CreationDateUtc { get; }

            public DateTime LastWriteDateUtc { get; }
        }
    }
}
