namespace Chunkyard.Infrastructure;

/// <summary>
/// An <see cref="IPrompt"/> which retrieves a password from an environment
/// variable.
/// </summary>
public sealed class EnvironmentPrompt : IPrompt
{
    public const string PasswordVariable = "CHUNKYARD_PASSWORD";

    public string NewPassword()
    {
        return Environment.GetEnvironmentVariable(PasswordVariable)
            ?? throw new InvalidOperationException(
                $"Environment variable \"{PasswordVariable}\" is empty or does not exist");
    }

    public string ExistingPassword()
    {
        return NewPassword();
    }
}
