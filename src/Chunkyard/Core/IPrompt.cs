namespace Chunkyard.Core;

/// <summary>
/// An interface to retrieve a user password.
/// </summary>
public interface IPrompt
{
    int Iterations { get; }

    string NewPassword();

    string ExistingPassword();
}
