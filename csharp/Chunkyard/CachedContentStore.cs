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

            _cacheDirectory = Path.Combine(cacheDirectory, hash);
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
            byte[] key,
            Stream outputStream)
        {
            _contentStore.RetrieveContent(contentReference, key, outputStream);
        }

        public T RetrieveContent<T>(
            ContentReference contentReference,
            byte[] key) where T : notnull
        {
            return _contentStore.RetrieveContent<T>(contentReference, key);
        }

        public ContentReference StoreContent(
            Stream inputStream,
            byte[] key,
            string contentName)
        {
            if (!(inputStream is FileStream fileStream))
            {
                return _contentStore.StoreContent(
                    inputStream,
                    key,
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
                key,
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
            byte[] key,
            string contentName) where T : notnull
        {
            return _contentStore.StoreContent<T>(value, key, contentName);
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

        public int AppendToLog<T>(T value, int? currentLogPosition)
            where T : notnull
        {
            return _contentStore.AppendToLog<T>(value, currentLogPosition);
        }

        public T RetrieveFromLog<T>(int logPosition)
            where T : notnull
        {
            return _contentStore.RetrieveFromLog<T>(logPosition);
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
