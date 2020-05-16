using System.Collections.Generic;

namespace Chunkyard
{
    public class NonceGenerator
    {
        private readonly Dictionary<string, byte[]> _noncesByFingerprints;

        public NonceGenerator()
        {
            _noncesByFingerprints = new Dictionary<string, byte[]>();
        }

        public void Register(string fingerprint, byte[] nonce)
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
