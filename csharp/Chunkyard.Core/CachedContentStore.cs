using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public class CachedContentStore<T> : IContentStore<T> where T : IContentRef
    {
        private readonly IContentStore<T> _store;
        private readonly string _cacheDirectory;

        public CachedContentStore(IContentStore<T> store, string cacheDirectory)
        {
            _store = store;
            _cacheDirectory = cacheDirectory;
        }

        public IRepository Repository
        {
            get
            {
                return _store.Repository;
            }
        }

        public T Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName)
        {
            if (!(stream is FileStream fileStream))
            {
                return _store.Store(stream, hashAlgorithmName, contentName);
            }

            var storedCache = RetrieveFromCache(contentName);

            if (storedCache != null
                && storedCache.Length == fileStream.Length
                && storedCache.CreationDateUtc.Equals(File.GetCreationTimeUtc(fileStream.Name))
                && storedCache.LastWriteDateUtc.Equals(File.GetLastWriteTimeUtc(fileStream.Name)))
            {
                return storedCache.ContentRef;
            }

            var contentRef = _store.Store(stream, hashAlgorithmName, contentName);

            StoreInCache(contentName, new Cache<T>(
                contentRef,
                fileStream.Length,
                File.GetCreationTimeUtc(fileStream.Name),
                File.GetLastWriteTimeUtc(fileStream.Name)));

            return contentRef;
        }

        public void Retrieve(Stream stream, T contentRef)
        {
            _store.Retrieve(stream, contentRef);
        }

        public IEnumerable<Uri> ListContentUris(T contentRef)
        {
            return _store.ListContentUris(contentRef);
        }

        public bool Valid(T contentRef)
        {
            return _store.Valid(contentRef);
        }

        public void Visit(T contentRef)
        {
            _store.Visit(contentRef);
        }

        private Cache<T>? RetrieveFromCache(string fileName)
        {
            var cacheFile = Path.Combine(_cacheDirectory, fileName);

            if (!File.Exists(cacheFile))
            {
                return null;
            }

            return DataConvert.DeserializeObject<Cache<T>>(
                File.ReadAllText(cacheFile));
        }

        private void StoreInCache(string fileName, Cache<T> cache)
        {
            var cacheFile = Path.Combine(_cacheDirectory, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile));

            File.WriteAllText(
                cacheFile,
                DataConvert.SerializeObject(cache));
        }

        private class Cache<TC> where TC : IContentRef
        {
            public Cache(TC contentRef, long length, DateTime creationDateUtc, DateTime lastWriteDateUtc)
            {
                ContentRef = contentRef;
                Length = length;
                CreationDateUtc = creationDateUtc;
                LastWriteDateUtc = lastWriteDateUtc;
            }

            public TC ContentRef { get; }

            public long Length { get; }

            public DateTime CreationDateUtc { get; }

            public DateTime LastWriteDateUtc { get; }
        }
    }
}

