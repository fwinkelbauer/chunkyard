using CommandLine;

namespace Chunkyard.Options
{
    [Verb("push", HelpText = "Pushes the content of a snapshot to a remote repository")]
    public class PushOptions
    {
        public PushOptions(string remote)
        {
            Remote = remote;
        }

        [Option('r', "remote", Required = true, HelpText = "The remote repository")]
        public string Remote { get; }
    }
}
