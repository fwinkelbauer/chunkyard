using System;
using System.IO;

namespace Chunkyard.Core
{
    public static class LzmaCompression
    {
        // https://stackoverflow.com/questions/7646328/how-to-use-the-7z-sdk-to-compress-and-decompress-a-file
        public static byte[] Compress(byte[] data)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();

            // Write the encoder properties
            coder.WriteCoderProperties(output);

            // Write the decompressed file size.
            for (int i = 0; i < 8; i++)
            {
                output.WriteByte((byte)(input.Length >> (8 * i)));
            }

            // Encode the file.
            coder.Code(input, output, input.Length, -1, null);

            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            SevenZip.Compression.LZMA.Decoder coder = new SevenZip.Compression.LZMA.Decoder();
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();

            // Read the decoder properties
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);

            // Read in the decompressed file size.
            byte[] fileLengthBytes = new byte[8];
            input.Read(fileLengthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

            coder.SetDecoderProperties(properties);
            coder.Code(input, output, input.Length, fileLength, null);

            return output.ToArray();
        }
    }
}
