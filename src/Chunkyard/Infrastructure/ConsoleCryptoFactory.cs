namespace Chunkyard.Infrastructure;

internal sealed class ConsoleCryptoFactory : ICryptoFactory
{
    public Crypto Create(SnapshotReference? snapshotReference)
    {
        if (snapshotReference == null)
        {
            return new Crypto(
                NewPassword(),
                RandomNumberGenerator.GetBytes(Crypto.SaltBytes),
                Crypto.DefaultIterations);
        }
        else
        {
            return new Crypto(
                ExistingPassword(),
                snapshotReference.Salt,
                snapshotReference.Iterations);
        }
    }

    private static string NewPassword()
    {
        var firstPassword = ReadPassword("Enter new password: ");
        var secondPassword = ReadPassword("Re-enter password: ");

        if (!firstPassword.Equals(secondPassword))
        {
            throw new InvalidOperationException("Passwords do not match");
        }

        return firstPassword;
    }

    private static string ExistingPassword()
    {
        return ReadPassword("Password: ");
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);

        var result = new StringBuilder();

        while (true)
        {
            var info = Console.ReadKey(true);

            if (info.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return result.ToString();
            }
            else if (info.Key == ConsoleKey.Backspace
                && result.Length > 0)
            {
                Console.Write("\b \b");
                result.Length--;
            }
            else if (!char.IsControl(info.KeyChar))
            {
                Console.Write("*");
                _ = result.Append(info.KeyChar);
            }
        }
    }
}
