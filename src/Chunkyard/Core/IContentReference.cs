using System;
using System.Collections.Generic;

namespace Chunkyard.Core
{
    /// <summary>
    /// A reference which describes how to retrieve encrypted and chunked data
    /// from a <see cref="ContentStore"/>.
    /// </summary>
    public interface IContentReference
    {
        public byte[] Nonce { get; }

        public IReadOnlyCollection<Uri> ContentUris { get; }
    }
}
