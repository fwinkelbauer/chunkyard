namespace Chunkyard.Infrastructure
{
    /// <summary>
    /// A decorator of <see cref="IPrompt"/> which retrieves a password from a
    /// set of environment variables. If these environment variables do not
    /// exist, the decorated <see cref="IPrompt"/> is called.
    /// </summary>
    internal class EnvironmentPrompt : IPrompt
    {
        private const string PasswordVariable = "CHUNKYARD_PASSWORD";
        private const string ProcessVariable = "CHUNKYARD_PASSCMD";

        private readonly IPrompt _prompt;

        public EnvironmentPrompt(IPrompt prompt)
        {
            _prompt = prompt;
        }

        public string NewPassword()
        {
            return GetEnvironmentPassword()
                ?? GetProcessPassword()
                ?? _prompt.NewPassword();
        }

        public string ExistingPassword()
        {
            return GetEnvironmentPassword()
                ?? GetProcessPassword()
                ?? _prompt.ExistingPassword();
        }

        private static string? GetEnvironmentPassword()
        {
            return Environment.GetEnvironmentVariable(PasswordVariable);
        }

        private static string? GetProcessPassword()
        {
            var command = Environment.GetEnvironmentVariable(ProcessVariable);

            if (string.IsNullOrEmpty(command))
            {
                return null;
            }

            var split = command.Split(" ", 2);
            var fileName = split[0];
            var arguments = split.Length > 1
                ? split[1]
                : "";

            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new ChunkyardException(
                    $"Could not run '{command}'");
            }

            var builder = new StringBuilder();
            string? line;

            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                builder.Append(line);
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new ChunkyardException(
                    $"Exit code of '{command}' was {process.ExitCode}");
            }

            return builder.ToString();
        }
    }
}
