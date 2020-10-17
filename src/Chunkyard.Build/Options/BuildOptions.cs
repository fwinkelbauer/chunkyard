﻿﻿using System.Runtime.InteropServices;
using CommandLine;

namespace Chunkyard.Build.Options
{
    [Verb("publish", HelpText = "Publish the main project.")]
    public class BuildOptions
    {
        private const string DefaultConfiguration = "Release";
        private const string DefaultRuntime = "";

        public BuildOptions(string configuration, string runtime)
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

        public BuildOptions()
            : this(DefaultConfiguration, DefaultRuntime)
        {
        }

        [Option('c', "configuration", Required = false, HelpText = "The build configuration", Default = DefaultConfiguration)]
        public string Configuration { get; }

        [Option('r', "runtime", Required = false, HelpText = "The build runtime")]
        public string Runtime { get; }
    }
}