using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Chunkyard.Core
{
    public class AesGcmContentStore<T> : IContentStore<AesGcmContentRef<T>> where T : IContentRef
    {
        private readonly IContentStore<T> _store;
        private readonly byte[] _key;
        private readonly Dictionary<string, byte[]> _noncesByName;

        public AesGcmContentStore(IContentStore<T> store, byte[] key)
        {
            _store = store;
            _key = key;
            _noncesByName = new Dictionary<string, byte[]>();
        }

        public AesGcmContentRef<T> Store(Stream stream, HashAlgorithmName hashAlgorithmName, string contentName)
        {
            var nonce = GetNonce(contentName);

            using var plaintextStream = new MemoryStream();
            stream.CopyTo(plaintextStream);
            var (ciphertext, tag) = Crypto.AesGcmEncrypt(plaintextStream.ToArray(), _key, nonce);

            using var ciphertextStream = new MemoryStream(ciphertext);
            return new AesGcmContentRef<T>(
                _store.Store(ciphertextStream, hashAlgorithmName, contentName),
                nonce,
                tag);
        }

        public void Retrieve(Stream stream, AesGcmContentRef<T> contentRef)
        {
            using var ciphertextStream = new MemoryStream();
            _store.Retrieve(ciphertextStream, contentRef.ContentRef);

            var ciphertext = ciphertextStream.ToArray();
            var plaintext = Crypto.AesGcmDecrypt(
                ciphertext,
                contentRef.Tag.ToArray(),
                _key,
                contentRef.Nonce.ToArray());

            stream.Write(plaintext);
        }

        public bool Valid(AesGcmContentRef<T> contentRef)
        {
            return _store.Valid(contentRef.ContentRef);
        }

        public void Visit(AesGcmContentRef<T> contentRef)
        {
            _noncesByName[contentRef.Name] = contentRef.Nonce.ToArray();
            _store.Visit(contentRef.ContentRef);
        }

        public IEnumerable<Uri> ListContentUris(AesGcmContentRef<T> contentRef)
        {
            return _store.ListContentUris(contentRef.ContentRef);
        }

        private byte[] GetNonce(string name)
        {
            if (_noncesByName.TryGetValue(name, out var nonce))
            {
                return nonce;
            }
            else
            {
                nonce = Crypto.GenerateNonce();
                _noncesByName[name] = nonce;

                return nonce;
            }
        }
    }
}
