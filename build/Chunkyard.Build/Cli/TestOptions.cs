namespace Chunkyard.Build.Cli;

[Verb("test", HelpText = "Test the solution.")]
public class TestOptions : DotnetOptions
{
    public TestOptions(string configuration)
        : base(configuration)
    {
    }
}
