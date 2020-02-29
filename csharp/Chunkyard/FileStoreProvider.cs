using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Chunkyard
{
    public class FileStoreProvider : IContentStoreProvider
    {
        private readonly string _directory;

        public FileStoreProvider(string directory)
        {
            _directory = directory;
        }

        public Uri Store(HashAlgorithmName algorithm, byte[] value)
        {
            var contentUri = Hash.ComputeContentUri(algorithm, value);

            if (!Exists(contentUri))
            {
                using var fileStream = new FileStream(
                    ToFilePath(contentUri),
                    FileMode.CreateNew);

                fileStream.Write(value);
            }

            return contentUri;
        }

        public byte[] Retrieve(Uri contentUri)
        {
            return File.ReadAllBytes(
                ToFilePath(contentUri));
        }

        public IEnumerable<Uri> List()
        {
            foreach (var subDirectory in Directory.GetDirectories(_directory))
            {
                foreach (var file in Directory.GetFiles(subDirectory))
                {
                    yield return Hash.ToContentUri(
                        Path.GetFileName(subDirectory),
                        Path.GetFileName(file));
                }
            }
        }

        public void Remove(Uri contentUri)
        {
            File.Delete(
                ToFilePath(contentUri));
        }

        public bool Exists(Uri contentUri)
        {
            return File.Exists(
                ToFilePath(contentUri));
        }

        private string ToFilePath(Uri contentUri)
        {
            var directoryPath = Path.Combine(
                _directory,
                Hash.AlgorithmFromContentUri(contentUri).Name);

            Directory.CreateDirectory(directoryPath);

            return Path.Combine(
                directoryPath,
                Hash.HashFromContentUri(contentUri));
        }
    }
}
