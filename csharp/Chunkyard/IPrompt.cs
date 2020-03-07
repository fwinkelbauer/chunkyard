namespace Chunkyard
{
    internal interface IPrompt
    {
        string NewPassword();

        string ExistingPassword();
    }
}
