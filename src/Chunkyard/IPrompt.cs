namespace Chunkyard
{
    /// <summary>
    /// An interface to retrieve a user password.
    /// </summary>
    public interface IPrompt
    {
        string NewPassword();

        string ExistingPassword();
    }
}
