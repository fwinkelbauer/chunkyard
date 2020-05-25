using CommandLine;

namespace Chunkyard.Build.Options
{
    [Verb("publish", HelpText = "Publishes the main project")]
    public class PublishOptions : DotnetOptions
    {
        public PublishOptions(string configuration, string runtime)
            : base(configuration, runtime)
        {
        }
    }
}
