namespace Chunkyard
{
    public interface IPrompt
    {
        string NewPassword();

        string ExistingPassword();
    }
}
