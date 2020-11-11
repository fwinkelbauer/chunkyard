using CommandLine;

namespace Chunkyard.Build.Options
{
    [Verb("clean", HelpText = "Clean the solution.")]
    public class CleanOptions : DotnetOptions
    {
        public CleanOptions(string configuration, string runtime)
            : base(configuration, runtime)
        {
        }
    }
}
