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
    private sealed record FuzzyExpression(Regex Regex, bool Negated);

    private readonly List<FuzzyExpression> _expressions;
    private readonly bool _initialMatch;

    public Fuzzy(params string[] patterns)
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

/// <summary>
/// Extension methods based on <see cref="Fuzzy"/>.
/// </summary>
public static class FuzzyExtensions
{
    public static IEnumerable<Blob> ListBlobs(
        this IBlobSystem blobSystem,
        Fuzzy? fuzzy)
    {
        fuzzy ??= new();

        return blobSystem.ListBlobs()
            .Where(b => fuzzy.IsMatch(b.Name));
    }

    public static Blob[] ListBlobs(
        this Snapshot snapshot,
        Fuzzy? fuzzy)
    {
        fuzzy ??= new();

        return snapshot.BlobReferences
            .Select(br => br.Blob)
            .Where(b => fuzzy.IsMatch(b.Name))
            .ToArray();
    }

    public static BlobReference[] ListBlobReferences(
        this Snapshot snapshot,
        Fuzzy? fuzzy)
    {
        fuzzy ??= new();

        return snapshot.BlobReferences
            .Where(b => fuzzy.IsMatch(b.Blob.Name))
            .ToArray();
    }
}
