using CommandLine;

namespace Chunkyard.Options
{
    [Verb("restore", HelpText = "Restores a snapshot")]
    public class RestoreOptions
    {
        public RestoreOptions(string directory, string includeRegex, string refLogId)
        {
            Directory = directory;
            IncludeRegex = includeRegex;
            RefLogId = refLogId;
        }

        [Option('d', "directory", Required = true, HelpText = "The restore directory")]
        public string Directory { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }

        [Option('r', "refLogId", Required = false, HelpText = "The refLog URI", Default = "")]
        public string RefLogId { get; }
    }
}
