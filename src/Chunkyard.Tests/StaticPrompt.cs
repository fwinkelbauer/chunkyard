namespace Chunkyard.Tests
{
    public class StaticPrompt : IPrompt
    {
        public string NewPassword()
        {
            return "secret";
        }

        public string ExistingPassword()
        {
            return "secret";
        }
    }
}
