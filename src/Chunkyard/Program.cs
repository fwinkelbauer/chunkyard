﻿using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Chunkyard.Cli;
using Chunkyard.Core;
using CommandLine;

namespace Chunkyard
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ProcessArguments(args);
            }
            catch (ChunkyardException e)
            {
                WriteError($"Error: {e.Message}");
            }
            catch (CryptographicException)
            {
                WriteError("Error: Could not decrypt data");
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
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
                .WithParsed<PreviewOptions>(Commands.PreviewFiles)
                .WithParsed<RestoreOptions>(Commands.RestoreSnapshot)
                .WithParsed<CreateOptions>(Commands.CreateSnapshot)
                .WithParsed<CheckOptions>(Commands.CheckSnapshot)
                .WithParsed<ShowOptions>(Commands.ShowSnapshot)
                .WithParsed<RemoveOptions>(Commands.RemoveSnapshot)
                .WithParsed<KeepOptions>(Commands.KeepSnapshots)
                .WithParsed<ListOptions>(Commands.ListSnapshots)
                .WithParsed<GarbageCollectOptions>(Commands.GarbageCollect)
                .WithParsed<CopyOptions>(Commands.CopySnapshots)
                .WithParsed<DotOptions>(Commands.Dot)
                .WithNotParsed(_ => Environment.ExitCode = 1);
        }
    }
}
