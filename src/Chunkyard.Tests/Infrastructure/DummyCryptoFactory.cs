namespace Chunkyard.Tests.Infrastructure;

/// <summary>
/// A <see cref="ICryptoFactory"/> implementation that returns a fixed crypto
/// key.
/// </summary>
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
            return new Crypto(_password, "2j+W/mBAdmFaWHRy", 10);
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
