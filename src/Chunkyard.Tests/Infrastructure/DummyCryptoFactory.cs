namespace Chunkyard.Tests.Infrastructure;

internal sealed class DummyCryptoFactory : ICryptoFactory
{
    private readonly string _password;

    public DummyCryptoFactory(string password)
    {
        _password = password;
    }

    public Crypto Create(SnapshotReference? snapshotReference)
    {
        if (snapshotReference == null)
        {
            return new Crypto(
                _password,
                RandomNumberGenerator.GetBytes(Crypto.SaltBytes),
                Crypto.DefaultIterations);
        }
        else
        {
            return new Crypto(
                _password,
                snapshotReference.Salt,
                snapshotReference.Iterations);
        }
    }
}
