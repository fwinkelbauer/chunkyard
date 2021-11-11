namespace Chunkyard.Build.Cli
{
    [Verb("publish", HelpText = "Publish the main project.")]
    public class PublishOptions : DotnetOptions
    {
        public PublishOptions(string configuration)
            : base(configuration)
        {
        }
    }
}
