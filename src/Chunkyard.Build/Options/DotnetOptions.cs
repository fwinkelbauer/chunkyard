using System;
﻿using System.Runtime.InteropServices;
using CommandLine;

namespace Chunkyard.Build.Options
{
    public abstract class DotnetOptions
    {
        private const string DefaultConfiguration = "Release";

        public DotnetOptions(string configuration, string runtime)
        {
            Configuration = configuration;

            Runtime = string.IsNullOrEmpty(runtime)
                ? FetchRuntimeIdentifier()
                : runtime;
        }

        [Option('c', "configuration", Required = false, HelpText = "The build configuration", Default = DefaultConfiguration)]
        public string Configuration { get; }

        [Option('r', "runtime", Required = false, HelpText = "The build runtime")]
        public string Runtime { get; }

        private static string FetchRuntimeIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx-x64";
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not infer runtime identifier");
            }
        }
    }
}
