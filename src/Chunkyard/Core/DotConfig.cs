using System.Collections.Immutable;

namespace Chunkyard.Core
{
    /// <summary>
    /// A configuration to automate a Chunkyard backup.
    /// </summary>
    public class DotConfig
    {
        public DotConfig(
            string repository,
            IImmutableList<string> files,
            IImmutableList<string>? excludePatterns,
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

        public IImmutableList<string> Files { get; }

        public IImmutableList<string>? ExcludePatterns { get; }

        public bool? Cached { get; }

        public int? LatestCount { get; }
    }
}
