using CommandLine;

namespace Chunkyard.Options
{
    [Verb("push", HelpText = "Pushes the content of a snapshot in a given log to a remote repository")]
    public class PushOptions
    {
        public PushOptions(string remote, string logName)
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
