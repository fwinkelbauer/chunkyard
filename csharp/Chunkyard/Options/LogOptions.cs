using CommandLine;

namespace Chunkyard.Options
{
    [Verb("log", HelpText = "Lists all entries in a content reference log")]
    public class LogOptions
    {
        public LogOptions(string logName)
        {
            LogName = logName;
        }

        [Option('l', "log", Required = false, HelpText = "The log name", Default = Command.DefaultLogName)]
        public string LogName { get; }
    }
}
