﻿using System;
using System.Linq;
using System.Reflection;
using Chunkyard.Build.Options;
using CommandLine;

namespace Chunkyard.Build
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ProcessArguments(args);
            }
            catch (Exception e)
            {
                WriteError($"Error: {e.Message}");
            }
        }

        private static void WriteError(string message)
        {
            Environment.ExitCode = 1;

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static void ProcessArguments(string[] args)
        {
            var optionTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();

            Parser.Default.ParseArguments(args, optionTypes)
                .WithParsed<CleanOptions>(o => Cli.Clean(o))
                .WithParsed<BuildOptions>(o => Cli.Build(o))
                .WithParsed<PublishOptions>(o => Cli.Publish(o))
                .WithParsed<ReleaseOptions>(_ => Cli.Release())
                .WithParsed<FmtOptions>(_ => Cli.Fmt())
                .WithParsed<UpgradeOptions>(_ => Cli.Upgrade())
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }
    }
}
