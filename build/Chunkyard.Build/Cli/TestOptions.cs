using CommandLine;

namespace Chunkyard.Build.Cli
{
    [Verb("test", HelpText = "Test the solution.")]
    public class TestOptions : DotnetOptions
    {
        public TestOptions(bool live, string configuration)
            : base(configuration)
        {
            Live = live;
        }

        [Option("live", Required = false, HelpText = "Run tests on every file change", Default = false)]
        public bool Live { get; }
    }
}
