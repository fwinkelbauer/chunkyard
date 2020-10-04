using System;
using System.IO;

namespace Chunkyard
{
    public abstract class DecoratorContentStore : IContentStore
    {
        public DecoratorContentStore(IContentStore store)
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

        public virtual (ContentReference ContentReference, bool IsNewContent) StoreContent(
            Stream inputStream,
            string contentName)
        {
            return Store.StoreContent(inputStream, contentName);
        }

        public virtual void RegisterContent(ContentReference contentReference)
        {
            Store.RegisterContent(contentReference);
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
            Guid logId,
            ContentReference contentReference,
            int newLogPosition)
        {
            return Store.AppendToLog(
                logId,
                contentReference,
                newLogPosition);
        }

        public virtual LogReference RetrieveFromLog(int logPosition)
        {
            return Store.RetrieveFromLog(logPosition);
        }
    }
}
