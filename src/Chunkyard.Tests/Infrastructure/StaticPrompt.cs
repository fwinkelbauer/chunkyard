using Chunkyard.Core;

namespace Chunkyard.Tests.Infrastructure
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
