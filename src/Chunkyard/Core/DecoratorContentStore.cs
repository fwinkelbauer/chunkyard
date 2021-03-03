using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// An abstract implementation of <see cref="IContentStore"/> which can be
    /// used to implement decorators.
    /// </summary>
    public abstract class DecoratorContentStore : IContentStore
    {
        protected DecoratorContentStore(IContentStore store)
        {
            Store = store;
        }

        public IRepository Repository => Store.Repository;

        protected IContentStore Store { get; }

        public virtual void RetrieveContent(
            ContentReference contentReference,
            byte[] key,
            Stream outputStream)
        {
            Store.RetrieveContent(contentReference, key, outputStream);
        }

        public virtual ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] key,
            byte[] nonce,
            ContentType type,
            out bool isNewContent)
        {
            return Store.StoreContent(
                inputStream,
                contentName,
                key,
                nonce,
                type,
                out isNewContent);
        }

        public virtual bool ContentExists(ContentReference contentReference)
        {
            return Store.ContentExists(contentReference);
        }

        public virtual bool ContentValid(ContentReference contentReference)
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
