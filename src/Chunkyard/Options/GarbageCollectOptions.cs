using CommandLine;

namespace Chunkyard.Options
{
    [Verb("gc", HelpText = "Remove unreferenced content.")]
    public class GarbageCollectOptions
    {
        public GarbageCollectOptions(
            string repository,
            bool whatIf)
        {
            Repository = repository;
            WhatIf = whatIf;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option("what-if", Required = false, HelpText = "Show what would happen", Default = false)]
        public bool WhatIf { get; }
    }
}
