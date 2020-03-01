using Newtonsoft.Json;

namespace Chunkyard.Core
{
    public class LzmaContentRef<T> : IContentRef where T : IContentRef
    {
        public LzmaContentRef(T contentRef)
        {
            ContentRef = contentRef;
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
    }
}
