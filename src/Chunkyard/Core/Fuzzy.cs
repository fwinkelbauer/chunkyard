namespace Chunkyard.Core;

/// <summary>
/// A fuzzy pattern matcher.
/// </summary>
public sealed class Fuzzy
{
    public static readonly Fuzzy Default = new Fuzzy(
        Array.Empty<string>());

    private readonly Regex[] _compiledRegex;

    public Fuzzy(IEnumerable<string> patterns)
    {
        _compiledRegex = patterns
            .Select(p => string.IsNullOrEmpty(p)
                ? ".*"
                : p.Replace(" ", ".*"))
            .Select(p => p.Any(char.IsUpper)
                ? p
                : $"(?i){p}")
            .Select(p => new Regex(p))
            .ToArray();
    }

    public bool IsIncludingMatch(string input)
    {
        return IsMatch(input, _compiledRegex.Length == 0);
    }

    public bool IsExcludingMatch(string input)
    {
        return IsMatch(input, false);
    }

    private bool IsMatch(string input, bool initial)
    {
        return _compiledRegex.Any(r => r.IsMatch(input))
            || initial;
    }
}
