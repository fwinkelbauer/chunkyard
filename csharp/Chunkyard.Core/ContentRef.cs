using System;

namespace Chunkyard.Core
{
    public class ContentRef : IContentRef
    {
        public ContentRef(string name, Uri uri)
        {
            Name = name;
            Uri = uri;
        }

        public string Name { get; }

        public Uri Uri { get; }
    }
}
