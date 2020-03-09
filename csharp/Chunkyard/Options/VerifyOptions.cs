﻿using CommandLine;

namespace Chunkyard.Options
{
    [Verb("verify", HelpText = "Verify a snapshot")]
    public class VerifyOptions
    {
        public VerifyOptions(string repository, string logId, string includeRegex)
        {
            Repository = repository;
            LogId = logId;
            IncludeRegex = includeRegex;
        }

        [Option('r', "repository", Required = false, HelpText = "The repository", Default = Command.RepositoryDirectoryName)]
        public string Repository { get; }

        [Option('l', "log-uri", Required = false, HelpText = "The log URI", Default = Command.DefaultLogId)]
        public string LogId { get; }

        [Option('i', "include", Required = false, HelpText = "The include regex", Default = ".*")]
        public string IncludeRegex { get; }
    }
}
