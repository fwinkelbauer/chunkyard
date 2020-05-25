using System.Runtime.InteropServices;
using CommandLine;

namespace Chunkyard.Build.Options
{
    public class DotnetOptions
    {
        public const string DefaultConfiguration = "Release";
        public const string DefaultRuntime = "";

        public DotnetOptions(string configuration, string runtime)
        {
            Configuration = configuration;

            if (string.IsNullOrEmpty(runtime))
            {
                Runtime = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "win-x64"
                    : "linux-x64";
            }
            else
            {
                Runtime = runtime;
            }
        }

        [Option('c', "configuration", Required = false, HelpText = "The build configuration", Default = DefaultConfiguration)]
        public string Configuration { get; }

        [Option('r', "runtime", Required = false, HelpText = "The build runtime")]
        public string Runtime { get; }
    }
}
