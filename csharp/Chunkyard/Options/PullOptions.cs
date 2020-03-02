using CommandLine;

namespace Chunkyard.Options
{
    [Verb("pull", HelpText = "Pulls the content of a snapshot in a given log from a remote repository")]
    public class PullOptions
    {
        public PullOptions(string remote, string logName)
        {
            Remote = remote;
            LogName = logName;
        }

        [Option('r', "remote", Required = true, HelpText = "The remote repository")]
        public string Remote { get; }

        [Option('l', "log", Required = false, HelpText = "The log name", Default = "")]
        public string LogName { get; }
    }
}
