using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public class ContentStore : IContentStore<ContentRef>
    {
        private readonly IRepository _repository;

        public ContentStore(IRepository repository)
        {
            _repository = repository;
        }

        public ContentRef Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            var uri = _repository.StoreContent(
                hashAlgorithmName,
                memoryStream.ToArray());

            return new ContentRef(
                contentName,
                uri);
        }

        public void Retrieve(Stream stream, ContentRef contentRef)
        {
            var content = _repository.RetrieveContent(
                contentRef.Uri);

            if (!IsContentValid(contentRef.Uri, content))
            {
                throw new ChunkyardException($"Invalid content: {contentRef.Name}");
            }

            stream.Write(content);
        }

        public bool Valid(ContentRef contentRef)
        {
            if (!_repository.ContentExists(contentRef.Uri))
            {
                return false;
            }

            var content = _repository.RetrieveContent(
                contentRef.Uri);

            return IsContentValid(contentRef.Uri, content);
        }

        public void Visit(ContentRef _)
        {
        }

        public IEnumerable<Uri> ListContentUris(ContentRef contentRef)
        {
            yield return contentRef.Uri;
        }

        private bool IsContentValid(Uri contentUri, byte[] content)
        {
            var computedUri = Id.ComputeContentUri(
                Id.AlgorithmFromContentUri(contentUri),
                content);

            return contentUri.Equals(computedUri);
        }
    }
}
