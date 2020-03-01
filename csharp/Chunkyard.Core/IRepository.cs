using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public interface IRepository
    {
        Uri StoreContent(HashAlgorithmName algorithm, byte[] value);

        byte[] RetrieveContent(Uri contentUri);

        bool ContentExists(Uri contentUri);

        int AppendToLog<T>(T contentRef, string logName, int? currentLogPosition) where T : IContentRef;

        T RetrieveFromLog<T>(string logName, int logPosition) where T : IContentRef;

        int? FetchLogPosition(string logName);

        IEnumerable<int> ListLog(string logName);
    }
}
