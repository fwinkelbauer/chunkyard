using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Chunkyard.Core;

namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// An implementation of <see cref="IRepository{T}"/> using a ZIP file.
    /// </summary>
    internal class ZipRepository<T> : IRepository<T>
    {
        private readonly object _lock;
        private readonly string _zipFile;
        private readonly string _subDirectory;
        private readonly Func<T, string> _toFile;
        private readonly Func<string, T> _fromFile;

        public ZipRepository(
            string zipFile,
            string subDirectory,
            Func<T, string> toFile,
            Func<string, T> fromFile)
        {
            _lock = new object();
            _zipFile = zipFile;
            _subDirectory = subDirectory;
            _toFile = toFile;
            _fromFile = fromFile;
        }

        public void StoreValue(T key, byte[] value)
        {
            lock (_lock)
            {
                using var zip = OpenZip();
                var file = ToFile(key);

                if (zip.Entries.Any(e => e.FullName == file))
                {
                    return;
                }

                var entry = zip.Entries
                    .FirstOrDefault(e => e.FullName == file)
                    ?? zip.CreateEntry(file);

                using var stream = entry.Open();

                stream.Write(value);
            }
        }

        public byte[] RetrieveValue(T key)
        {
            lock (_lock)
            {
                using var zip = OpenZip();
                var entry = zip.Entries.First(e => e.FullName == ToFile(key));

                using var reader = entry.Open();
                using var memoryStream = new MemoryStream();

                reader.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public bool ValueExists(T key)
        {
            lock (_lock)
            {
                using var zip = OpenZip();

                return zip.Entries.Any(e => e.FullName == ToFile(key));
            }
        }

        public IReadOnlyCollection<T> ListKeys()
        {
            lock (_lock)
            {
                if (!File.Exists(_zipFile))
                {
                    return Array.Empty<T>();
                }

                using var zip = OpenZip();

                return zip.Entries
                    .Where(e => e.FullName.StartsWith(_subDirectory))
                    .Select(e => FromFile(e.FullName))
                    .ToArray();
            }
        }

        public void RemoveValue(T key)
        {
            lock (_lock)
            {
                using var zip = OpenZip();

                zip.Entries.First(e => e.FullName == ToFile(key))
                    .Delete();
            }
        }

        private ZipArchive OpenZip()
        {
            return ZipFile.Open(_zipFile, ZipArchiveMode.Update);
        }

        private string ToFile(T key)
        {
            return Path.Combine(
                _subDirectory,
                _toFile(key));
        }

        private T FromFile(string file)
        {
            return _fromFile(
                Path.GetRelativePath(_subDirectory, file));
        }
    }

    public static class ZipRepository
    {
        public static IRepository<Uri> CreateUriRepository(
            string zipFile,
            string subDirectory)
        {
            return new ZipRepository<Uri>(
                zipFile,
                subDirectory,
                contentUri =>
                {
                    var (algorithm, hash) = Id.DeconstructContentUri(
                        contentUri);

                    return Path.Combine(
                        algorithm.ToLower(),
                        hash.Substring(0, 2),
                        hash);
                },
                file =>
                {
                    return Id.ToContentUri(
                        DirectoryUtil.GetParent(
                            DirectoryUtil.GetParent(file)),
                        Path.GetFileNameWithoutExtension(file));
                });
        }

        public static IRepository<int> CreateIntRepository(
            string zipFile,
            string subDirectory)
        {
            return new ZipRepository<int>(
                zipFile,
                subDirectory,
                number => number.ToString(),
                file => Convert.ToInt32(file));
        }
    }
}
