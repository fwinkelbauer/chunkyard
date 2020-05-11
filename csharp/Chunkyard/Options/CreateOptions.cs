﻿using CommandLine;

namespace Chunkyard.Options
{
    [Verb("create", HelpText = "Creates a new snapshot")]
    public class CreateOptions
    {
        public CreateOptions(string repository, bool cached)
        {
            Repository = repository;
            Cached = cached;
        }

        [Option('r', "repository", Required = true, HelpText = "The repository")]
        public string Repository { get; }

        [Option('c', "cached", Required = false, HelpText = "Use a file cache to improve performance", Default = false)]
        public bool Cached { get; }
    }
}