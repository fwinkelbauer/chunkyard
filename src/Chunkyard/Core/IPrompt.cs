namespace Chunkyard.Core;

/// <summary>
/// An interface to retrieve a user password.
/// </summary>
public interface IPrompt
{
    string NewPassword(string repositoryId);

    string ExistingPassword(string repositoryId);
}
