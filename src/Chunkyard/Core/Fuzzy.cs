using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Chunkyard.Core
{
    /// <summary>
    /// A fuzzy pattern matcher.
    /// </summary>
    public class Fuzzy
    {
        public static readonly Fuzzy MatchAll = new Fuzzy(
            Array.Empty<string>(),
            true);

        public static readonly Fuzzy MatchNothing = new Fuzzy(
            Array.Empty<string>(),
            false);

        private readonly Regex[] _compiledRegex;
        private readonly bool _initial;

        public Fuzzy(
            IEnumerable<string> patterns,
            bool emptyMatches)
        {
            _compiledRegex = patterns
                .Select(p => new Regex(string.IsNullOrEmpty(p)
                    ? ".*"
                    : p.Replace(" ", ".*")))
                .ToArray();

            _initial = _compiledRegex.Length == 0 && emptyMatches;
        }

        public bool IsMatch(string input)
        {
            return _compiledRegex
                .Select(r => r.IsMatch(input))
                .Aggregate(_initial, (total, next) => total | next);
        }
    }
}
