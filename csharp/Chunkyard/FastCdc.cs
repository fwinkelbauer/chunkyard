using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Chunkyard
{
    internal static class FastCdc
    {
        private const string ProcessName = "chunker";

        public static IEnumerable<byte[]> SplitIntoChunks(
            Stream stream,
            int minChunkSizeInByte,
            int avgChunkSizeInByte,
            int maxChunkSizeInByte)
        {
            // Starting the chunker process is expensive, so we're only
            // running it on files that are large enough
            if (stream is FileStream fileStream
                && fileStream.Length > maxChunkSizeInByte)
            {
                var cuts = ComputeCuts(
                    fileStream.Name,
                    minChunkSizeInByte,
                    avgChunkSizeInByte,
                    maxChunkSizeInByte);

                foreach (var cut in cuts)
                {
                    var buffer = new byte[cut];
                    fileStream.Read(buffer, 0, buffer.Length);

                    yield return buffer;
                }
            }
            else
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);

                yield return memoryStream.ToArray();
            }
        }

        private static IEnumerable<int> ComputeCuts(
            string file,
            int minChunkSizeInByte,
            int avgChunkSizeInByte,
            int maxChunkSizeInByte)
        {
            var startInfo = new ProcessStartInfo(
                ProcessName,
                $"\"{file}\" {minChunkSizeInByte} {avgChunkSizeInByte} {maxChunkSizeInByte}")
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
                throw new ChunkyardException(
                    $"Exit code of {ProcessName} was {chunker.ExitCode}");
            }
        }
    }
}
