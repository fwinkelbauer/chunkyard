using System.Collections.Generic;

namespace Chunkyard
{
    public class EncryptionProvider
    {
        private readonly Dictionary<string, byte[]> _noncesByFingerprints;

        private string? _password;
        private byte[]? _salt;
        private int? _iterations;

        public EncryptionProvider()
        {
            _noncesByFingerprints = new Dictionary<string, byte[]>();

            _salt = null;
            _iterations = null;
        }

        public string? Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }

        public byte[] Salt
        {
            get
            {
                if (_salt == null)
                {
                    _salt = AesGcmCrypto.GenerateSalt();
                }

                return _salt;
            }
            set
            {
                _salt = value;
            }
        }

        public int Iterations
        {
            get
            {
                if (_iterations == null)
                {
                    _iterations = AesGcmCrypto.Iterations;
                }

                return _iterations.Value;
            }
            set
            {
                _iterations = value;
            }
        }

        public void RegisterNonce(string fingerprint, byte[] nonce)
        {
            _noncesByFingerprints[fingerprint] = nonce;
        }

        public byte[] GetNonce(string fingerprint)
        {
            if (!_noncesByFingerprints.TryGetValue(fingerprint, out var nonce))
            {
                nonce = AesGcmCrypto.GenerateNonce();
                _noncesByFingerprints[fingerprint] = nonce;
            }

            return nonce;
        }
    }
}
