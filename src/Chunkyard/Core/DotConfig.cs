using System.Collections.Generic;

namespace Chunkyard.Core
{
    /// <summary>
    /// A configuration to automate a Chunkyard backup.
    /// </summary>
    public class DotConfig
    {
        public DotConfig(
            string repository,
            IReadOnlyCollection<string> files,
            IReadOnlyCollection<string>? excludePatterns,
            bool? cached,
            int? latestCount)
        {
            Repository = repository;
            Files = files;
            ExcludePatterns = excludePatterns;
            Cached = cached;
            LatestCount = latestCount;
        }

        public string Repository { get; }

        public IReadOnlyCollection<string> Files { get; }

        public IReadOnlyCollection<string>? ExcludePatterns { get; }

        public bool? Cached { get; }

        public int? LatestCount { get; }
    }
}
