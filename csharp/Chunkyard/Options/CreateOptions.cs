using CommandLine;

namespace Chunkyard.Options
{
    [Verb("create", HelpText = "Creates a new snapshot for a given log")]
    public class CreateOptions
    {
        public CreateOptions(string logName)
        {
            LogName = logName;
        }

        [Option('l', "log", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }
    }
}
