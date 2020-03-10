using System;
using System.Text;
namespace Chunkyard
{
    internal class ConsolePrompt : IPrompt
    {
        public string NewPassword()
        {
            var firstPassword = ReadPassword("Enter new password: ");
            var secondPassword = ReadPassword("Re-enter password: ");

            if (!firstPassword.Equals(secondPassword))
            {
                throw new ChunkyardException("Passwords do not match");
            }

            return firstPassword;
        }

        public string ExistingPassword()
        {
            return ReadPassword("Password: ");
        }

        // https://stackoverflow.com/questions/23433980/c-sharp-console-hide-the-input-from-console-window-while-typing
        private static string ReadPassword(string prompt)
        {
            Console.Write(prompt);

            var input = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace
                    && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                }
                else
                {
                    input.Append(key.KeyChar);
                }
            }

            Console.WriteLine();

            return input.ToString();
        }
    }
}
