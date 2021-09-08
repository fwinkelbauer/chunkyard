using System.IO;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IBlobWriter"/> using the file system.
    /// </summary>
    internal class FileBlobWriter : IBlobWriter
    {
        private readonly string _directory;

        public FileBlobWriter(string directory)
        {
            _directory = directory;
        }

        public Blob? FindBlob(string blobName)
        {
            var file = Path.Combine(_directory, blobName);

            return File.Exists(file)
                ? new Blob(
                    blobName,
                    File.GetLastWriteTimeUtc(file))
                : null;
        }

        public Stream OpenWrite(string blobName)
        {
            var file = Path.Combine(_directory, blobName);

            DirectoryUtil.CreateParent(file);

            return new FileStream(file, FileMode.Create, FileAccess.Write);
        }

        public void UpdateBlobMetadata(Blob blob)
        {
            var file = Path.Combine(_directory, blob.Name);

            File.SetLastWriteTimeUtc(file, blob.LastWriteTimeUtc);
        }
    }
}
