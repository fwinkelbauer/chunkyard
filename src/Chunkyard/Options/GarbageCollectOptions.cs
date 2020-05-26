using CommandLine;

namespace Chunkyard.Options
{
    [Verb("gc", HelpText = "Remove unreferenced content.")]
    public class GarbageCollectOptions
    {
        public GarbageCollectOptions(
            string repository,
            bool preview)
        {
            Repository = repository;
            Preview = preview;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('p', "preview", Required = false, HelpText = "Print instead of delete", Default = false)]
        public bool Preview { get; }
    }
}
