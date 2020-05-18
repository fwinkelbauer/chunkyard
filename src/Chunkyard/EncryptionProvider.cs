using System.Collections.Generic;
using System.Linq;

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

        public IEnumerable<byte> Salt
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
                _salt = value.ToArray();
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

        public void RegisterNonce(string fingerprint, IEnumerable<byte> nonce)
        {
            _noncesByFingerprints[fingerprint] = nonce.ToArray();
        }

        public IEnumerable<byte> GetNonce(string fingerprint)
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
