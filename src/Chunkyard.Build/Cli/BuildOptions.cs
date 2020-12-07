using CommandLine;

namespace Chunkyard.Build.Cli
{
    [Verb("build", HelpText = "Build the solution.")]
    public class BuildOptions : DotnetOptions
    {
        public BuildOptions(string configuration, string runtime)
            : base(configuration, runtime)
        {
        }
    }
}
