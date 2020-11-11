using System.IO;

namespace Chunkyard
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

        protected IContentStore Store { get; }

        public virtual int? CurrentLogPosition => Store.CurrentLogPosition;

        public virtual void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            Store.RetrieveContent(contentReference, outputStream);
        }

        public virtual ContentReference StoreContent(
            Stream inputStream,
            string contentName,
            byte[] nonce,
            ContentType type,
            out bool newContent)
        {
            return Store.StoreContent(
                inputStream,
                contentName,
                nonce,
                type,
                out newContent);
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
            ContentReference contentReference)
        {
            return Store.AppendToLog(
                newLogPosition,
                contentReference);
        }

        public virtual LogReference RetrieveFromLog(int logPosition)
        {
            return Store.RetrieveFromLog(logPosition);
        }
    }
}
