﻿using System;
using System.IO;

namespace Chunkyard
{
    /// <summary>
    /// An abstract implementation of <see cref="IContentStore"/> which can be
    /// used to implement decorators.
    /// </summary>
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
            string contentName,
            byte[] nonce,
            ContentType type)
        {
            return Store.StoreContent(inputStream, contentName, nonce, type);
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
