namespace Chunkyard.Core;

/// <summary>
/// An interface to retrieve a user password.
/// </summary>
public interface IPrompt
{
    string NewPassword(string key);

    string ExistingPassword(string key);
}
