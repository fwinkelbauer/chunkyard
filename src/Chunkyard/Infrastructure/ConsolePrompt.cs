namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> which prompts the user for a
/// password using the console.
/// </summary>
internal sealed class ConsolePrompt : IPrompt
{
    public string NewPassword(string key)
    {
        var firstPassword = ReadPassword("Enter new password: ");
        var secondPassword = ReadPassword("Re-enter password: ");

        if (!firstPassword.Equals(secondPassword))
        {
            throw new InvalidOperationException("Passwords do not match");
        }

        return firstPassword;
    }

    public string ExistingPassword(string key)
    {
        return ReadPassword("Password: ");
    }

    // https://stackoverflow.com/questions/3404421/password-masking-console-application
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
