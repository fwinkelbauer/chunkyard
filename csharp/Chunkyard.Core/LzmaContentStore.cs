using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public class LzmaContentStore<T> : IContentStore<LzmaContentRef<T>> where T : IContentRef
    {
        private readonly IContentStore<T> _store;

        public LzmaContentStore(IContentStore<T> store)
        {
            _store = store;
        }

        public IRepository Repository
        {
            get
            {
                return _store.Repository;
            }
        }

        public LzmaContentRef<T> Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName)
        {
            using var compressedStream = new MemoryStream();
            CompressLzma(stream, compressedStream);
            compressedStream.Position = 0;

            return new LzmaContentRef<T>(
                _store.Store(compressedStream, hashAlgorithmName, contentName));
        }

        public void Retrieve(Stream stream, LzmaContentRef<T> contentRef)
        {
            using var compressedStream = new MemoryStream();
            _store.Retrieve(compressedStream, contentRef.ContentRef);
            compressedStream.Position = 0;

            DecompressLzma(compressedStream, stream);
        }

        public bool Valid(LzmaContentRef<T> contentRef)
        {
            return _store.Valid(contentRef.ContentRef);
        }

        public void Visit(LzmaContentRef<T> contentRef)
        {
            _store.Visit(contentRef.ContentRef);
        }

        public IEnumerable<Uri> ListContentUris(LzmaContentRef<T> contentRef)
        {
            return _store.ListContentUris(contentRef.ContentRef);
        }

        // https://stackoverflow.com/questions/7646328/how-to-use-the-7z-sdk-to-compress-and-decompress-a-file
        private static void CompressLzma(Stream input, Stream output)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();

            // Write the encoder properties
            coder.WriteCoderProperties(output);

            // Write the decompressed file size.
            for (int i = 0; i < 8; i++)
            {
                output.WriteByte((byte)(input.Length >> (8 * i)));
            }

            // Encode the file.
            coder.Code(input, output, input.Length, -1, null);
        }

        private static void DecompressLzma(Stream input, Stream output)
        {
            SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();

            // Read the decoder properties
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);

            // Read in the decompressed file size.
            byte[] fileLengthBytes = new byte[8];
            input.Read(fileLengthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

            coder.SetDecoderProperties(properties);
            coder.Code(input, output, input.Length, fileLength, null);
        }
    }
}
