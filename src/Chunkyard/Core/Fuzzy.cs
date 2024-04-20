namespace Chunkyard.Core;

/// <summary>
/// A fuzzy pattern matcher.
///
/// "Fuzzy" expressions are regular expressions with extra features:
/// - Lowercase expressions are treated as "ignore case"
/// - Spaces are treated as "match all"
/// - A "!" prefix is treated as an exclude expressions
/// - Later expressions can overwrite previous expressions
///
/// The test suite contains a few examples.
/// </summary>
public sealed class Fuzzy
{
    public static readonly Fuzzy Default = new();

    private sealed record FuzzyExpression(Regex Regex, bool Negated);

    private readonly List<FuzzyExpression> _expressions;
    private readonly bool _initialMatch;

    public Fuzzy(params string[] patterns)
        : this((IEnumerable<string>)patterns)
    {
    }

    public Fuzzy(IEnumerable<string> patterns)
    {
        _expressions = new();

        foreach (var pattern in patterns)
        {
            var tmp = pattern.Trim();
            var negated = tmp.StartsWith('!');

            tmp = negated
                ? tmp[1..]
                : tmp;

            tmp = tmp.Replace(" ", ".*");

            tmp = tmp.Any(char.IsUpper)
                ? tmp
                : $"(?i){tmp}";

            _expressions.Add(
                new FuzzyExpression(
                    new Regex(tmp, RegexOptions.None, TimeSpan.FromSeconds(1)),
                    negated));
        }

        _initialMatch = _expressions.Count == 0
            || _expressions.First().Negated;
    }

    public bool IsMatch(string input)
    {
        var match = _initialMatch;

        foreach (var expression in _expressions)
        {
            if (expression.Negated && expression.Regex.IsMatch(input))
            {
                match = false;
            }
            else if (expression.Regex.IsMatch(input))
            {
                match = true;
            }
        }

        return match;
    }
}
