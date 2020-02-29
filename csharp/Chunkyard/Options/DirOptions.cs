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

        [Option('r', "refLogId", Required = false, HelpText = "The refLog URI", Default = "")]
        public string RefLogId { get; }
    }
}
