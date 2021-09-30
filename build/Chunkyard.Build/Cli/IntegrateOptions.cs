using CommandLine;

namespace Chunkyard.Build.Cli
{
    [Verb("integrate", isDefault: true, HelpText = "Build and test the solution.")]
    public class IntegrateOptions : DotnetOptions
    {
        public IntegrateOptions(string configuration)
            : base(configuration)
        {
        }
    }
}
