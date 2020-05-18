﻿using System.Text.RegularExpressions;

namespace Chunkyard
{
    public class Fuzzy
    {
        private readonly Regex _compiledRegex;

        public Fuzzy(string pattern)
        {
            _compiledRegex = new Regex(string.IsNullOrEmpty(pattern)
                ? ".*"
                : pattern.Replace(" ", ".*"));
        }

        public bool IsMatch(string input)
        {
            return _compiledRegex.IsMatch(input);
        }
    }
}