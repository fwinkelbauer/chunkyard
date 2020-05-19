﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Chunkyard
{
    internal class ContentStore : IContentStore
    {
        private const string DefaultLogName = "master";

        private static readonly object Lock = new object();

        private readonly IRepository _repository;
        private readonly FastCdc _fastCdc;
        private readonly HashAlgorithmName _hashAlgorithmName;
        private readonly byte[] _salt;
        private readonly int _iterations;
        private readonly byte[] _key;

        private readonly Dictionary<string, byte[]> _noncesByFile;

        public ContentStore(
            IRepository repository,
            FastCdc fastCdc,
            HashAlgorithmName hashAlgorithmName,
            string password,
            byte[] salt,
            int iterations)
        {
            _repository = repository;
            _fastCdc = fastCdc;
            _hashAlgorithmName = hashAlgorithmName;
            _salt = salt;
            _iterations = iterations;
            _key = AesGcmCrypto.PasswordToKey(password, salt, iterations);

            _noncesByFile = new Dictionary<string, byte[]>();
        }

        public Uri StoreUri
        {
            get
            {
                return _repository.RepositoryUri;
            }
        }

        public void RetrieveContent(
            ContentReference contentReference,
            Stream outputStream)
        {
            foreach (var chunk in contentReference.Chunks)
            {
                var decryptedData = AesGcmCrypto.Decrypt(
                    _repository.RetrieveContent(chunk.ContentUri),
                    chunk.Tag,
                    _key,
                    contentReference.Nonce);

                outputStream.Write(decryptedData);
            }
        }

        public T RetrieveContent<T>(ContentReference contentReference)
            where T : notnull
        {
            using var memoryStream = new MemoryStream();
            RetrieveContent(
                contentReference,
                memoryStream);

            return ToObject<T>(memoryStream.ToArray());
        }

        public ContentReference StoreContent(
            Stream inputStream,
            string contentName)
        {
            var nonce = GetNonce(contentName);

            return new ContentReference(
                contentName,
                nonce,
                WriteChunks(nonce, inputStream));
        }

        public ContentReference StoreContent<T>(T value, string contentName)
            where T : notnull
        {
            using var memoryStream = new MemoryStream(ToBytes(value));
            return StoreContent(
                (Stream)memoryStream, contentName);
        }

        public bool ContentExists(ContentReference contentReference)
        {
            var exists = true;

            foreach (var chunk in contentReference.Chunks)
            {
                exists &= _repository.ContentExists(chunk.ContentUri);
            }

            return exists;
        }

        public bool ContentValid(ContentReference contentReference)
        {
            var valid = true;

            foreach (var chunk in contentReference.Chunks)
            {
                valid &= _repository.ContentValid(chunk.ContentUri);
            }

            return valid;
        }

        public int? FetchLogPosition()
        {
            return FetchLogPosition(_repository);
        }

        public int AppendToLog(
            ContentReference contentReference,
            int? currentLogPosition)
        {
            var logReference = new LogReference(
                contentReference,
                _salt,
                _iterations);

            return _repository.AppendToLog(
                ToBytes(logReference),
                DefaultLogName,
                currentLogPosition);
        }

        public LogReference RetrieveFromLog(int logPosition)
        {
            return RetrieveFromLog(_repository, logPosition);
        }

        public IEnumerable<int> ListLogPositions()
        {
            return _repository.ListLogPositions(DefaultLogName);
        }

        public void RegisterNonce(string name, byte[] nonce)
        {
            _noncesByFile[name] = nonce;
        }

        private byte[] GetNonce(string name)
        {
            lock (Lock)
            {
                if (!_noncesByFile.TryGetValue(name, out var nonce))
                {
                    nonce = AesGcmCrypto.GenerateNonce();
                    _noncesByFile[name] = nonce;
                }

                return nonce;
            }
        }

        public static int? FetchLogPosition(
            IRepository repository)
        {
            return repository.FetchLogPosition(DefaultLogName);
        }

        public static LogReference RetrieveFromLog(
            IRepository repository,
            int logPosition)
        {
            return ToObject<LogReference>(
                repository.RetrieveFromLog(DefaultLogName, logPosition));
        }

        private static byte[] ToBytes(object o)
        {
            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(o));
        }

        private static T ToObject<T>(byte[] value) where T : notnull
        {
            return JsonConvert.DeserializeObject<T>(
                Encoding.UTF8.GetString(value));
        }

        private IEnumerable<ChunkReference> WriteChunks(
            byte[] nonce,
            Stream stream)
        {
            var chunkedDataItems = _fastCdc.SplitIntoChunks(stream);

            foreach (var chunkedData in chunkedDataItems)
            {
                var (encryptedData, tag) = AesGcmCrypto.Encrypt(
                    chunkedData,
                    _key,
                    nonce);

                var contentUri = Id.ComputeContentUri(
                    _hashAlgorithmName,
                    encryptedData);

                _repository.StoreContent(contentUri, encryptedData);

                yield return new ChunkReference(
                    contentUri,
                    tag);
            }
        }
    }
}
