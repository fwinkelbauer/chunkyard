using System;
using System.IO;

namespace Chunkyard.Core
{
    /// <summary>
    /// Defines a named block of binary data with some meta data.
    /// </summary>
    public class Blob
    {
        private readonly Func<Stream> _openRead;

        public Blob(
            Func<Stream> openRead,
            string name,
            DateTime creationTimeUtc,
            DateTime lastWriteTimeUtc)
        {
            _openRead = openRead;

            Name = name;
            CreationTimeUtc = creationTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public string Name { get; }

        public DateTime CreationTimeUtc { get; }

        public DateTime LastWriteTimeUtc { get; }

        public Stream OpenRead()
        {
            return _openRead();
        }
    }
}
