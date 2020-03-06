using System;
using System.IO;
using Chunkyard.Core;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal class CachedContentStore : IContentStore
    {
        private readonly IContentStore _contentStore;
        private readonly string _cacheDirectory;

        public CachedContentStore(IContentStore contentStore, string cacheDirectory)
        {
            _contentStore = contentStore;
            _cacheDirectory = cacheDirectory;
        }

        public IRepository Repository
        {
            get
            {
                return _contentStore.Repository;
            }
        }

        public void RetrieveContent(ContentReference contentReference, Stream stream, byte[] key)
        {
            _contentStore.RetrieveContent(contentReference, stream, key);
        }

        public ContentReference StoreContent(Stream stream, string contentName, byte[] nonce, byte[] key)
        {
            if (!(stream is FileStream fileStream))
            {
                return _contentStore.StoreContent(stream, contentName, nonce, key);
            }

            var storedCache = RetrieveFromCache(contentName);

            if (storedCache != null
                && storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(File.GetCreationTimeUtc(fileStream.Name))
                && storedCache.LastWriteDateUtc.Equals(File.GetLastWriteTimeUtc(fileStream.Name)))
            {
                return storedCache.ContentReference;
            }

            var contentReference = _contentStore.StoreContent(stream, contentName, nonce, key);

            StoreInCache(
                contentName,
                new Cache(
                    contentReference,
                    fileStream.Length,
                    File.GetCreationTimeUtc(fileStream.Name),
                    File.GetLastWriteTimeUtc(fileStream.Name)));

            return contentReference;
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
            return Path.Combine(_cacheDirectory, contentName);
        }

        private class Cache
        {
            public Cache(ContentReference contentReference, long length, DateTime creationDateUtc, DateTime lastWriteDateUtc)
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
