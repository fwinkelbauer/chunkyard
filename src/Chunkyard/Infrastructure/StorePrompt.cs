namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IPrompt"/> which stores credentials using
/// the file system.
/// </summary>
internal sealed class StorePrompt : IPrompt
{
    private static readonly string CredentialsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "chunkyard");

    private readonly IPrompt _prompt;

    public StorePrompt(IPrompt prompt)
    {
        _prompt = prompt;
    }

    public string NewPassword(string key)
    {
        return Store(key, _prompt.NewPassword(key));
    }

    public string ExistingPassword(string key)
    {
        var file = ToFile(key);

        return File.Exists(file)
            ? File.ReadAllText(file)
            : Store(key, _prompt.ExistingPassword(key));
    }

    private static string Store(string key, string password)
    {
        Directory.CreateDirectory(CredentialsDirectory);
        File.WriteAllText(ToFile(key), password);

        return password;
    }

    private static string ToFile(string key)
    {
        return Path.Combine(CredentialsDirectory, key);
    }
}
