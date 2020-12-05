using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Options
{
    [Verb("preview", HelpText = "Show all files matching the given terms.")]
    public class PreviewOptions
    {
        public PreviewOptions(
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            Files = files;
            ExcludePatterns = excludePatterns;
        }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
        public IEnumerable<string> ExcludePatterns { get; }
    }
}
