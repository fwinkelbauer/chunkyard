﻿using System.Collections.Generic;
using Chunkyard.Core;
using CommandLine;

namespace Chunkyard.Cli
{
    [Verb("create", HelpText = "Create a new snapshot and perform a shallow check.")]
    public class CreateOptions
    {
        public CreateOptions(
            string repository,
            IEnumerable<string> files,
            IEnumerable<string> excludePatterns,
            bool cached)
        {
            Repository = repository;
            Files = files;
            ExcludePatterns = excludePatterns;
            Cached = cached;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository path")]
        public string Repository { get; }

        [Option('f', "files", Required = true, HelpText = "The files and directories to include")]
        public IEnumerable<string> Files { get; }

        [Option('e', "exclude", Required = false, HelpText = "The fuzzy patterns for files to exclude")]
        public IEnumerable<string> ExcludePatterns { get; }

        [Option("cached", Required = false, HelpText = "Use a file cache to improve performance", Default = false)]
        public bool Cached { get; }
    }
}
