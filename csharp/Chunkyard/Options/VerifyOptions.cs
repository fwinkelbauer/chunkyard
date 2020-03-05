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

        [Option('r', "refLogId", Required = false, HelpText = "The reference log URI")]
        public string RefLogId { get; }
    }
}
