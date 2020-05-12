using System.Collections.Generic;
using CommandLine;

namespace Chunkyard.Options
{
    [Verb("preview", HelpText = "Show all files matching the given terms")]
    public class PreviewOptions
    {
        public PreviewOptions(
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns)
        {
            Files = new List<string>(files);
            ExcludePatterns = excludePatterns == null
                ? new List<string>()
                : new List<string>(excludePatterns);
        }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The exclude fuzzy patterns")]
        public IEnumerable<string> ExcludePatterns { get; }
    }
}
