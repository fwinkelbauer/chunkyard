﻿using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// A set of extension methods to work with <see cref="IContentStore"/>.
    /// </summary>
    public static class IContentStoreExtensions
    {
        public static T RetrieveContentObject<T>(
            this IContentStore store,
            ContentReference contentReference)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream();

            store.RetrieveContent(
                contentReference,
                memoryStream);

            return DataConvert.ToObject<T>(memoryStream.ToArray());
        }

        public static StoreResult StoreContentObject<T>(
            this IContentStore store,
            T value,
            string contentName)
            where T : notnull
        {
            store.EnsureNotNull(nameof(store));

            using var memoryStream = new MemoryStream(
                DataConvert.ToBytes(value));

            return store.StoreContent(
                memoryStream,
                contentName);
        }
    }
}
