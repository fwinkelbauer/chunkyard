using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public interface IContentStore<T> where T : IContentRef
    {
        IRepository Repository { get; }

        T Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName);

        void Retrieve(Stream stream, T contentRef);

        bool Valid(T contentRef);

        void Visit(T contentRef);

        IEnumerable<Uri> ListContentUris(T contentRef);
    }
}
