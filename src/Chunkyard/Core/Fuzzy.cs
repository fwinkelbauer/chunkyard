﻿using System;
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
            FuzzyOption.EmptyMatchesAll);

        public static readonly Fuzzy MatchNothing = new Fuzzy(
            Array.Empty<string>(),
            FuzzyOption.EmptyMatchesNothing);

        private readonly Regex[] _compiledRegex;
        private readonly bool _initial;

        public Fuzzy(
            IEnumerable<string> patterns,
            FuzzyOption option)
        {
            _compiledRegex = patterns
                .Select(p => new Regex(string.IsNullOrEmpty(p)
                    ? ".*"
                    : p.Replace(" ", ".*")))
                .ToArray();

            switch (option)
            {
                case FuzzyOption.EmptyMatchesAll:
                    _initial = _compiledRegex.Length == 0;
                    break;
                case FuzzyOption.EmptyMatchesNothing:
                    _initial = false;
                    break;
                default:
                    var name = Enum.GetName(typeof(FuzzyOption), option);

                    throw new NotSupportedException(
                        $"Unknown FuzzyOption: {name}");
            }
        }

        public bool IsMatch(string input)
        {
            return _compiledRegex
                .Select(r => r.IsMatch(input))
                .Aggregate(_initial, (total, next) => total | next);
        }
    }
}
