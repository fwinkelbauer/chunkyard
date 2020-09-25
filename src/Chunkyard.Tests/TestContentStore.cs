using System;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard.Tests
{
    public class TestContentStore : IContentStore
    {
        private readonly IContentStore _store;

        public TestContentStore(IRepository? repository = null)
        {
            _store = new ContentStore(
                repository ?? new MemoryRepository(),
                new FastCdc(
                    2 * 1024 * 1024,
                    4 * 1024 * 1024,
                    8 * 1024 * 1024),
                HashAlgorithmName.SHA256,
                new StaticPrompt());
        }

        public int? CurrentLogPosition => _store.CurrentLogPosition;

        public virtual void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            _store.RetrieveContent(contentReference, outputStream);
        }

        public virtual ContentReference StoreContent(
            Stream inputStream,
            string contentName)
        {
            return _store.StoreContent(inputStream, contentName);
        }

        public virtual ContentReference StoreContent(
            Stream inputStream,
            ContentReference previousContentReference)
        {
            return _store.StoreContent(
                inputStream,
                previousContentReference);
        }

        public virtual bool ContentExists(ContentReference contentReference)
        {
            return _store.ContentExists(contentReference);
        }

        public virtual bool ContentValid(ContentReference contentReference)
        {
            return _store.ContentValid(contentReference);
        }

        public virtual int AppendToLog(
            Guid logId,
            ContentReference contentReference,
            int newLogPosition)
        {
            return _store.AppendToLog(
                logId,
                contentReference,
                newLogPosition);
        }

        public virtual LogReference RetrieveFromLog(int logPosition)
        {
            return _store.RetrieveFromLog(logPosition);
        }
    }
}
