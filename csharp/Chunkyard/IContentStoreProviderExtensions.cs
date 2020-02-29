using System;

namespace Chunkyard
{
    public static class IContentStoreProviderExtensions
    {
        public static bool Valid(this IContentStoreProvider provider, Uri contentUri, out byte[] content)
        {
            if (!provider.Exists(contentUri))
            {
                content = Array.Empty<byte>();
                return false;
            }

            content = provider.Retrieve(contentUri);
            var computedUri = Hash.ComputeContentUri(
                Hash.AlgorithmFromContentUri(contentUri),
                content);

            return computedUri.Equals(contentUri);
        }

        public static bool Valid(this IContentStoreProvider provider, Uri contentUri)
        {
            return provider.Valid(contentUri, out var _);
        }
    }
}
