﻿using System;
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

        // https://stackoverflow.com/questions/3404421/password-masking-console-application
        public static string ReadPassword(string prompt)
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
}