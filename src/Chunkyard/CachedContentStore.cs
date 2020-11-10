﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace Chunkyard
{
    /// <summary>
    /// A decorator of <see cref="IContentStore"/> which remembers stored files
    /// in a separate cache. This cache can be used to skip store operations if
    /// the files (based on their meta data) have not changed.
    /// </summary>
    internal class CachedContentStore : DecoratorContentStore
    {
        private readonly string _cacheDirectory;

        public CachedContentStore(
            IContentStore contentStore,
            string cacheDirectory)
            : base(contentStore)
        {
            _cacheDirectory = cacheDirectory;
        }

        public override ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] nonce,
            ContentType type,
            out bool newContent)
        {
            if (!(inputStream is FileStream fileStream))
            {
                return Store.StoreContent(
                    inputStream,
                    contentName,
                    nonce,
                    type,
                    out newContent);
            }

            var storedReference = RetrieveFromCache(fileStream, contentName);

            if (storedReference != null)
            {
                newContent = false;
                return storedReference;
            }

            var contentReference = Store.StoreContent(
                inputStream,
                contentName,
                nonce,
                type,
                out newContent);

            StoreInCache(
                fileStream,
                contentReference);

            return contentReference;
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
                && Store.ContentExists(storedCache.ContentReference))
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

            DirectoryUtil.CreateParent(cacheFile);

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
