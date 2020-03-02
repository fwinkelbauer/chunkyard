using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public class FastCdcContentStore<T> : IContentStore<FastCdcContentRef<T>> where T : IContentRef
    {
        private readonly IContentStore<T> _store;
        private readonly int _minChunkSizeInByte;
        private readonly int _avgChunkSizeInByte;
        private readonly int _maxChunkSizeInByte;
        private readonly string _tempDirectory;

        public FastCdcContentStore(IContentStore<T> store, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte, string tempDirectory)
        {
            _store = store;
            _minChunkSizeInByte = minChunkSizeInByte;
            _avgChunkSizeInByte = avgChunkSizeInByte;
            _maxChunkSizeInByte = maxChunkSizeInByte;
            _tempDirectory = tempDirectory;
        }

        public IRepository Repository
        {
            get
            {
                return _store.Repository;
            }
        }

        public FastCdcContentRef<T> Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName)
        {
            if (stream is FileStream fileStream)
            {
                // Starting the chunker process is expensive, so we're only
                // running it on files that are large enough
                if (fileStream.Length <= _maxChunkSizeInByte)
                {
                    return new FastCdcContentRef<T>(
                        contentName,
                        new[] { _store.Store(fileStream, hashAlgorithmName, contentName) });
                }

                return new FastCdcContentRef<T>(
                    contentName,
                    StoreChunks(stream, hashAlgorithmName, contentName, contentName));
            }
            else
            {
                Directory.CreateDirectory(_tempDirectory);

                var tempFile = Path.Combine(
                    _tempDirectory,
                    Path.GetRandomFileName());

                try
                {
                    using (var writeStream = File.OpenWrite(tempFile))
                    {
                        stream.CopyTo(writeStream);
                    }

                    using var readStream = File.OpenRead(tempFile);
                    return new FastCdcContentRef<T>(
                        contentName,
                        StoreChunks(readStream, hashAlgorithmName, tempFile, contentName));
                }
                finally
                {
                    File.Delete(tempFile);
                }
            }
        }

        public void Retrieve(Stream stream, FastCdcContentRef<T> contentRef)
        {
            foreach (var chunkRef in contentRef.ChunkedContentRefs)
            {
                _store.Retrieve(stream, chunkRef);
            }
        }

        public bool Valid(FastCdcContentRef<T> contentRef)
        {
            var valid = true;

            foreach (var chunkRef in contentRef.ChunkedContentRefs)
            {
                valid &= _store.Valid(chunkRef);
            }

            return valid;
        }

        public void Visit(FastCdcContentRef<T> contentRef)
        {
            foreach (var chunkRef in contentRef.ChunkedContentRefs)
            {
                _store.Visit(chunkRef);
            }
        }

        public IEnumerable<Uri> ListContentUris(FastCdcContentRef<T> contentRef)
        {
            foreach (var chunkRef in contentRef.ChunkedContentRefs)
            {
                foreach (var chunkUris in _store.ListContentUris(chunkRef))
                {
                    yield return chunkUris;
                }
            }
        }

        private IEnumerable<T> StoreChunks(Stream stream, HashAlgorithmName hashAlgorithmName, string filePath, string contentName)
        {
            foreach (var chunk in ComputeChunks(filePath))
            {
                var buffer = new byte[chunk];
                stream.Read(buffer, 0, buffer.Length);
                using var chunkedStream = new MemoryStream(buffer);

                yield return _store.Store(chunkedStream, hashAlgorithmName, contentName);
            }
        }

        private IEnumerable<int> ComputeChunks(string filePath)
        {
            const string processName = "chunker";
            var startInfo = new ProcessStartInfo(
                processName,
                $"\"{filePath}\" {_minChunkSizeInByte} {_avgChunkSizeInByte} {_maxChunkSizeInByte}")
            {
                RedirectStandardOutput = true
            };

            using var chunker = Process.Start(startInfo);
            string? line = string.Empty;

            while ((line = chunker.StandardOutput.ReadLine()) != null)
            {
                yield return Convert.ToInt32(line);
            }

            chunker.WaitForExit();

            if (chunker.ExitCode != 0)
            {
                throw new ChunkyardException($"Exit code of {processName} was {chunker.ExitCode}");
            }
        }
    }
}
