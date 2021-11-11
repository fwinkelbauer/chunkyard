namespace Chunkyard.Build.Cli
{
    public abstract class DotnetOptions
    {
        protected DotnetOptions(string configuration)
        {
            Configuration = configuration;
        }

        [Option('c', "configuration", Required = false, HelpText = "The build configuration", Default = "Release")]
        public string Configuration { get; }
    }
}
