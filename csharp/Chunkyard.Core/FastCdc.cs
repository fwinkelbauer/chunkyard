using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Chunkyard.Core
{
    public static class FastCdc
    {
        private const string ProcessName = "chunker";

        public static IEnumerable<byte[]> SplitIntoChunks(Stream stream, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte, string tempDirectory)
        {
            if (stream is FileStream fileStream)
            {
                // Starting the chunker process is expensive, so we're only
                // running it on files that are large enough
                if (fileStream.Length <= maxChunkSizeInByte)
                {
                    using var memoryBuffer = new MemoryStream();
                    stream.CopyTo(memoryBuffer);

                    return new[] { memoryBuffer.ToArray() };
                }
                else
                {
                    return ComputeChunks(stream, fileStream.Name, minChunkSizeInByte, avgChunkSizeInByte, maxChunkSizeInByte);
                }
            }
            else
            {
                Directory.CreateDirectory(tempDirectory);

                var tempFile = Path.Combine(
                    tempDirectory,
                    "temp");

                try
                {
                    using (var writeStream = File.Create(tempFile))
                    {
                        stream.CopyTo(writeStream);
                    }

                    using var readStream = File.OpenRead(tempFile);
                    return SplitIntoChunks(
                        readStream,
                        minChunkSizeInByte,
                        avgChunkSizeInByte,
                        maxChunkSizeInByte,
                        tempDirectory);
                }
                finally
                {
                    File.Delete(tempFile);
                }
            }
        }

        private static IEnumerable<byte[]> ComputeChunks(Stream stream, string filePath, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte)
        {
            var cuts = ComputeCuts(
                filePath,
                minChunkSizeInByte,
                avgChunkSizeInByte,
                maxChunkSizeInByte);

            foreach (var cut in cuts)
            {
                var buffer = new byte[cut];
                stream.Read(buffer, 0, buffer.Length);

                yield return buffer;
            }
        }

        private static IEnumerable<int> ComputeCuts(string filePath, int minChunkSizeInByte, int avgChunkSizeInByte, int maxChunkSizeInByte)
        {
            var startInfo = new ProcessStartInfo(
                ProcessName,
                $"\"{filePath}\" {minChunkSizeInByte} {avgChunkSizeInByte} {maxChunkSizeInByte}")
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
                throw new ChunkyardException($"Exit code of {ProcessName} was {chunker.ExitCode}");
            }
        }
    }
}
