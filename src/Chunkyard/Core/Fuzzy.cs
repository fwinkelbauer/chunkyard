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
        public static readonly Fuzzy IncludeAll = Include(
            Array.Empty<string>());

        public static readonly Fuzzy ExcludeNothing = Exclude(
            Array.Empty<string>());

        private readonly Regex[] _compiledRegex;
        private readonly bool _initial;

        private Fuzzy(
            IEnumerable<string> patterns,
            bool emptyShouldMatch)
        {
            _compiledRegex = patterns
                .Select(p => new Regex(string.IsNullOrEmpty(p)
                    ? ".*"
                    : p.Replace(" ", ".*")))
                .ToArray();

            _initial = emptyShouldMatch
                ? _compiledRegex.Length == 0
                : false;
        }

        public bool IsMatch(string input)
        {
            return _compiledRegex
                .Select(r => r.IsMatch(input))
                .Aggregate(_initial, (total, next) => total | next);
        }

        public static Fuzzy Include(IEnumerable<string> patterns)
        {
            return new Fuzzy(patterns, true);
        }

        public static Fuzzy Exclude(IEnumerable<string> patterns)
        {
            return new Fuzzy(patterns, false);
        }
    }
}
