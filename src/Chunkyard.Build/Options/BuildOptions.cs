using CommandLine;

namespace Chunkyard.Build.Options
{
    [Verb("build", HelpText = "Build the main project.")]
    public class BuildOptions : DotnetOptions
    {
        public BuildOptions(string configuration, string runtime)
            : base(configuration, runtime)
        {
        }
    }
}
