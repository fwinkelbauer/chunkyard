namespace Chunkyard.Core;

/// <summary>
/// A fuzzy pattern matcher.
/// </summary>
public sealed class Fuzzy
{
    public static readonly Fuzzy Default = new(
        Array.Empty<string>());

    private record FuzzyExpression(Regex Regex, bool Negated);

    private readonly List<FuzzyExpression> _expressions;

    public Fuzzy(IEnumerable<string> patterns)
    {
        _expressions = new List<FuzzyExpression>();

        foreach (var pattern in patterns)
        {
            var tmp = pattern.Trim();
            var negated = tmp.StartsWith("!");

            tmp = negated
                ? tmp[1..]
                : tmp;

            tmp = tmp.Replace(" ", ".*");

            tmp = tmp.Any(char.IsUpper)
                ? tmp
                : $"(?i){tmp}";

            _expressions.Add(
                new FuzzyExpression(new Regex(tmp), negated));
        }
    }

    public bool IsMatch(string input)
    {
        var match = _expressions.Count == 0
            || _expressions.Count > 1 && _expressions.First().Negated;

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
