using CommandLine;

namespace Chunkyard.Build.Options
{
    [Verb("test", HelpText = "Test the solution")]
    public class TestOptions : DotnetOptions
    {
        public TestOptions(string configuration, string runtime)
            : base(configuration, runtime)
        {
        }
    }
}
