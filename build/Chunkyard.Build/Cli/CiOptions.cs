using CommandLine;

namespace Chunkyard.Build.Cli
{
    [Verb("ci", HelpText = "Build and test the solution.")]
    public class CiOptions : DotnetOptions
    {
        public CiOptions(string configuration)
            : base(configuration)
        {
        }
    }
}
