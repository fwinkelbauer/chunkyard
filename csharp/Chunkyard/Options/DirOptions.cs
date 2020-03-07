using CommandLine;

namespace Chunkyard.Options
{
    [Verb("dir", HelpText = "Lists all files in a snapshot")]
    public class DirOptions
    {
        public DirOptions(string includeRegex, string refLogId)
        {
            IncludeRegex = includeRegex;
            RefLogId = refLogId;
        }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('r', "reflog", Required = false, HelpText = "The reference log URI", Default = Command.DefaultRefLog)]
        public string RefLogId { get; }
    }
}
