using System.Collections.Generic;
using Newtonsoft.Json;

namespace Chunkyard.Core
{
    public class AesGcmContentRef<T> : IContentRef where T : IContentRef
    {
        public AesGcmContentRef(T contentRef, byte[] nonce, byte[] tag)
        {
            ContentRef = contentRef;
            Nonce = nonce;
            Tag = tag;
        }

        [JsonIgnore]
        public string Name
        {
            get
            {
                return ContentRef.Name;
            }
        }

        public T ContentRef { get; }

        public IReadOnlyCollection<byte> Nonce { get; }

        public IReadOnlyCollection<byte> Tag { get; }
    }
}
