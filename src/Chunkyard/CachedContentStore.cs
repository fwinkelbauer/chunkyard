using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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

            // Each repository should have its own cache
            var hash = Id.ComputeHash(
                HashAlgorithmName.SHA256,
                _contentStore.Repository.RepositoryUri.AbsoluteUri);

            var shortHash = hash.Substring(0, 8);

            _cacheDirectory = Path.Combine(cacheDirectory, shortHash);
        }

        public IRepository Repository => _contentStore.Repository;

        public void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            _contentStore.RetrieveContent(contentReference, outputStream);
        }

        public T RetrieveContentObject<T>(ContentReference contentReference)
            where T : notnull
        {
            return _contentStore.RetrieveContentObject<T>(contentReference);
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

            var storedCache = RetrieveFromCache(contentName);
            var creationDateUtc = File.GetCreationTimeUtc(fileStream.Name);
            var lastWriteDateUtc = File.GetLastWriteTimeUtc(fileStream.Name);

            if (storedCache != null
                && storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(creationDateUtc)
                && storedCache.LastWriteDateUtc.Equals(lastWriteDateUtc)
                && _contentStore.ContentExists(storedCache.ContentReference))
            {
                return storedCache.ContentReference;
            }

            var contentReference = _contentStore.StoreContent(
                inputStream,
                contentName);

            StoreInCache(
                contentReference.Name,
                new Cache(
                    contentReference,
                    fileStream.Length,
                    creationDateUtc,
                    lastWriteDateUtc));

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

            var storedCache = RetrieveFromCache(
                previousContentReference.Name);

            var creationDateUtc = File.GetCreationTimeUtc(fileStream.Name);
            var lastWriteDateUtc = File.GetLastWriteTimeUtc(fileStream.Name);

            if (storedCache != null
                && storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(creationDateUtc)
                && storedCache.LastWriteDateUtc.Equals(lastWriteDateUtc)
                && _contentStore.ContentExists(storedCache.ContentReference))
            {
                return storedCache.ContentReference;
            }

            var contentReference = _contentStore.StoreContent(
                inputStream,
                previousContentReference);

            StoreInCache(
                contentReference.Name,
                new Cache(
                    contentReference,
                    fileStream.Length,
                    creationDateUtc,
                    lastWriteDateUtc));

            return contentReference;
        }

        public ContentReference StoreContentObject<T>(
            T value,
            string contentName)
            where T : notnull
        {
            return _contentStore.StoreContentObject<T>(value, contentName);
        }

        public ContentReference StoreContentObject<T>(
            T value,
            ContentReference previousContentReference)
            where T : notnull
        {
            return _contentStore.StoreContentObject<T>(
                value,
                previousContentReference);
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
            int newLogPosition)
        {
            return _contentStore.AppendToLog(
                contentReference,
                newLogPosition);
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return _contentStore.RetrieveFromLog(logPosition);
        }

        public void RemoveFromLog(int logPosition)
        {
            _contentStore.RemoveFromLog(logPosition);
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
