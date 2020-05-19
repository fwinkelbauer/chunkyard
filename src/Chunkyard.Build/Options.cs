using CommandLine;

namespace Chunkyard.Build
{
    public class Options
    {
        public Options(
            string target,
            string configuration,
            string runtime)
        {
            Target = target;
            Configuration = configuration;
            Runtime = runtime;
        }

        [Option('t', "target", Required = true, HelpText = "The target")]
        public string Target { get; }

        [Option('c', "configuration", Required = true, HelpText = "The configuration")]
        public string Configuration { get; }

        [Option('r', "runtime", Required = true, HelpText = "The runtime")]
        public string Runtime { get; }
    }
}
