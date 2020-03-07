using CommandLine;

namespace Chunkyard.Options
{
    [Verb("verify", HelpText = "Verify a snapshot")]
    public class VerifyOptions
    {
        public VerifyOptions(string refLogId)
        {
            RefLogId = refLogId;
        }

        [Option('r', "reflog", Required = false, HelpText = "The reference log URI", Default = Command.DefaultRefLog)]
        public string RefLogId { get; }
    }
}
