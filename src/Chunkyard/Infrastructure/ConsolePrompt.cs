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
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();

                    return result.ToString();
                case ConsoleKey.Backspace:
                    if (result.Length == 0)
                    {
                        continue;
                    }

                    result.Length--;
                    Console.Write("\b \b");

                    continue;
                default:
                    result.Append(key.KeyChar);
                    Console.Write("*");

                    continue;
            }
        }
    }
}
