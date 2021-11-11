namespace Chunkyard.Build.Cli
{
    [Verb("clean", HelpText = "Clean the solution.")]
    public class CleanOptions : DotnetOptions
    {
        public CleanOptions(string configuration)
            : base(configuration)
        {
        }
    }
}
