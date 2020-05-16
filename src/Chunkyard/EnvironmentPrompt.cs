using System;
using System.Text;
namespace Chunkyard
{
    internal class EnvironmentPrompt : IPrompt
    {
        private const string PasswordVariable = "CHUNKYARD_PASSWORD";

        private readonly IPrompt _prompt;

        public EnvironmentPrompt(IPrompt prompt)
        {
            _prompt = prompt;
        }

        public string NewPassword()
        {
            if (TryGetPassword(out var password))
            {
                return password;
            }

            return _prompt.NewPassword();
        }

        public string ExistingPassword()
        {
            if (TryGetPassword(out var password))
            {
                return password;
            }

            return _prompt.ExistingPassword();
        }

        private bool TryGetPassword(out string password)
        {
            password = Environment.GetEnvironmentVariable(PasswordVariable)
                ?? string.Empty;

            return !string.IsNullOrEmpty(password);
        }
    }
}
