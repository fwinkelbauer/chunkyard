using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Chunkyard.Core
{
    public class AesGcmContentRef<T> : IContentRef where T : IContentRef
    {
        public AesGcmContentRef(T contentRef, byte[] nonce, byte[] tag)
        {
            ContentRef = contentRef;
            Nonce = nonce.ToImmutableArray();
            Tag = tag.ToImmutableArray();
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

        public ImmutableArray<byte> Nonce { get; }

        public ImmutableArray<byte> Tag { get; }
    }
}
