using System;
using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a contract to store data and return a reference to retrieve that
    /// stored data.
    /// </summary>
    public interface IContentStore
    {
        void RetrieveBlob(
            BlobReference blobReference,
            byte[] key,
            Stream outputStream);

        T RetrieveDocument<T>(
            DocumentReference documentReference,
            byte[] key)
            where T : notnull;

        BlobReference StoreBlob(
            Blob blob,
            byte[] key,
            byte[] nonce,
            Stream inputStream);

        DocumentReference StoreDocument<T>(
            T value,
            byte[] key,
            byte[] nonce)
            where T : notnull;

        bool ContentExists(IContentReference contentReference);

        bool ContentValid(IContentReference contentReference);

        Uri[] ListContentUris();

        void RemoveContent(Uri contentUri);
    }
}
