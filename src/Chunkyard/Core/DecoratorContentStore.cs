using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// An abstract implementation of <see cref="IContentStore"/> which can be
    /// used to implement decorators.
    /// </summary>
    internal abstract class DecoratorContentStore : IContentStore
    {
        protected DecoratorContentStore(IContentStore store)
        {
            Store = store;
        }

        public IRepository Repository => Store.Repository;

        protected IContentStore Store { get; }

        public virtual void RetrieveBlob(
            BlobReference blobReference,
            byte[] key,
            Stream outputStream)
        {
            Store.RetrieveBlob(blobReference, key, outputStream);
        }

        public virtual T RetrieveDocument<T>(
            DocumentReference documentReference,
            byte[] key)
            where T : notnull
        {
            return Store.RetrieveDocument<T>(documentReference, key);
        }

        public virtual BlobReference StoreBlob(
            Blob blob,
            byte[] key,
            byte[] nonce)
        {
            return Store.StoreBlob(blob, key, nonce);
        }

        public virtual DocumentReference StoreDocument<T>(
            T value,
            byte[] key,
            byte[] nonce)
            where T : notnull
        {
            return Store.StoreDocument<T>(value, key, nonce);
        }

        public virtual bool ContentExists(IContentReference contentReference)
        {
            return Store.ContentExists(contentReference);
        }

        public virtual bool ContentValid(IContentReference contentReference)
        {
            return Store.ContentValid(contentReference);
        }

        public virtual int AppendToLog(
            int newLogPosition,
            LogReference logReference)
        {
            return Store.AppendToLog(
                newLogPosition,
                logReference);
        }

        public virtual LogReference RetrieveFromLog(int logPosition)
        {
            return Store.RetrieveFromLog(logPosition);
        }
    }
}
