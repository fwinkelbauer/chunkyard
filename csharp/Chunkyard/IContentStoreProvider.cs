using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Chunkyard
{
    public interface IContentStoreProvider
    {
        Uri Store(HashAlgorithmName algorithm, byte[] value);

        byte[] Retrieve(Uri contentUri);

        IEnumerable<Uri> List();

        void Remove(Uri contentUri);

        bool Exists(Uri contentUri);
    }
}
