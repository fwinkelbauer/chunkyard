using System.Text.RegularExpressions;

namespace Chunkyard
{
    internal class Fuzzy
    {
        private readonly Regex _compiledRegex;

        public Fuzzy(string pattern)
        {
            _compiledRegex = new Regex(pattern
                .EnsureNotNull(nameof(pattern))
                .Replace(" ", ".*"));
        }

        public bool IsMatch(string input)
        {
            return _compiledRegex.IsMatch(input);
        }
    }
}
