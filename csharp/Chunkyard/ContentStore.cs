using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard
{
    public class ContentStore : IContentStore<ContentRef>
    {
        private readonly IRepository _storeProvider;

        public ContentStore(IRepository storeProvider)
        {
            _storeProvider = storeProvider;
        }

        public ContentRef Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            var uri = _storeProvider.StoreContent(
                hashAlgorithmName,
                memoryStream.ToArray());

            return new ContentRef(
                contentName,
                uri);
        }

        public void Retrieve(Stream stream, ContentRef contentRef)
        {
            if (!_storeProvider.ValidContent(contentRef.Uri, out var content))
            {
                throw new ChunkyardException($"Invalid content: {contentRef.Name}");
            }

            stream.Write(content);
        }

        public bool Valid(ContentRef contentRef)
        {
            return _storeProvider.ValidContent(
                contentRef.Uri);
        }

        public void Visit(ContentRef _)
        {
        }

        public IEnumerable<Uri> ListContentUris(ContentRef contentRef)
        {
            yield return contentRef.Uri;
        }
    }
}
