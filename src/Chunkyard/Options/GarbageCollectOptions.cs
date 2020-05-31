using CommandLine;

namespace Chunkyard.Options
{
    [Verb("gc", HelpText = "Remove unreferenced content.")]
    public class GarbageCollectOptions
    {
        public GarbageCollectOptions(string repository)
        {
            Repository = repository;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }
    }
}
