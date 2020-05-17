using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal class CachedContentStore : IContentStore
    {
        private readonly IContentStore _contentStore;
        private readonly string _cacheDirectory;

        public CachedContentStore(
            IContentStore contentStore,
            string cacheDirectory)
        {
            _contentStore = contentStore;

            // Each repository should have its own cache
            var hash = Id.ComputeHash(
                HashAlgorithmName.SHA256,
                _contentStore.StoreUri.AbsoluteUri);

            var shortHash = hash.Substring(0, 8);

            _cacheDirectory = Path.Combine(cacheDirectory, shortHash);
        }

        public Uri StoreUri
        {
            get
            {
                return _contentStore.StoreUri;
            }
        }

        public void RetrieveContent(
            ContentReference contentReference,
            ContentStoreConfig config,
            Stream outputStream)
        {
            _contentStore.RetrieveContent(contentReference, config, outputStream);
        }

        public T RetrieveContent<T>(
            ContentReference contentReference,
            ContentStoreConfig config) where T : notnull
        {
            return _contentStore.RetrieveContent<T>(contentReference, config);
        }

        public ContentReference StoreContent(
            Stream inputStream,
            ContentStoreConfig config,
            string contentName)
        {
            if (!(inputStream is FileStream fileStream))
            {
                return _contentStore.StoreContent(
                    inputStream,
                    config,
                    contentName);
            }

            var storedCache = RetrieveFromCache(contentName);

            if (storedCache != null
                && storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(
                    File.GetCreationTimeUtc(fileStream.Name))
                && storedCache.LastWriteDateUtc.Equals(
                    File.GetLastWriteTimeUtc(fileStream.Name)))
            {
                return storedCache.ContentReference;
            }

            var contentReference = _contentStore.StoreContent(
                inputStream,
                config,
                contentName);

            StoreInCache(
                contentName,
                new Cache(
                    contentReference,
                    fileStream.Length,
                    File.GetCreationTimeUtc(fileStream.Name),
                    File.GetLastWriteTimeUtc(fileStream.Name)));

            return contentReference;
        }

        public ContentReference StoreContent<T>(
            T value,
            ContentStoreConfig config,
            string contentName) where T : notnull
        {
            return _contentStore.StoreContent<T>(value, config, contentName);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            return _contentStore.ContentExists(contentReference);
        }

        public bool ContentValid(ContentReference contentReference)
        {
            return _contentStore.ContentValid(contentReference);
        }

        public int? FetchLogPosition()
        {
            return _contentStore.FetchLogPosition();
        }

        public int AppendToLog(
            ContentReference contentReference,
            int? currentLogPosition)
        {
            return _contentStore.AppendToLog(
                contentReference,
                currentLogPosition);
        }

        public ContentReference RetrieveFromLog(int logPosition)
        {
            return _contentStore.RetrieveFromLog(logPosition);
        }

        public IEnumerable<int> ListLogPositions()
        {
            return _contentStore.ListLogPositions();
        }

        private Cache? RetrieveFromCache(string contentName)
        {
            var cacheFile = ToCacheFile(contentName);

            if (!File.Exists(cacheFile))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Cache>(
                File.ReadAllText(cacheFile));
        }

        private void StoreInCache(string contentName, Cache cache)
        {
            var cacheFile = ToCacheFile(contentName);

            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile));

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
