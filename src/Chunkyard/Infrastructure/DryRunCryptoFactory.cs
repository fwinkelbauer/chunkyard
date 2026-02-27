namespace Chunkyard.Infrastructure;

/// <summary>
/// A <see cref="ICryptoFactory"/> decorator that generates a password for empty
/// repositories.
/// </summary>
public sealed class DryRunCryptoFactory : ICryptoFactory
{
    private readonly ICryptoFactory _cryptoFactory;

    public DryRunCryptoFactory(ICryptoFactory cryptoFactory)
    {
        _cryptoFactory = cryptoFactory;
    }

    public Crypto Create(SnapshotReference? snapshotReference)
    {
        if (snapshotReference == null)
        {
            return new Crypto(Guid.NewGuid().ToString());
        }
        else
        {
            return _cryptoFactory.Create(snapshotReference);
        }
    }
}
