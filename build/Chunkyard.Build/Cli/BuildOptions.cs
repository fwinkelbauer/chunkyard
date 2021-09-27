using CommandLine;

namespace Chunkyard.Build.Cli
{
    [Verb("build", HelpText = "Build the solution.")]
    public class BuildOptions : DotnetOptions
    {
        public BuildOptions(bool liveTest, string configuration)
            : base(configuration)
        {
            LiveTest = liveTest;
        }

        [Option("live-test", Required = false, HelpText = "Run tests on every file change", Default = false)]
        public bool LiveTest { get; }
    }
}
