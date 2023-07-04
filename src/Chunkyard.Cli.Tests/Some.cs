namespace Chunkyard.Cli.Tests;

internal static class Some
{
    public static Dictionary<TKey, TValue> Dict<TKey, TValue>(
        params (TKey Key, TValue Value)[] pairs)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>();

        foreach (var pair in pairs)
        {
            dict.Add(pair.Key, pair.Value);
        }

        return dict;
    }

    public static IReadOnlyCollection<string> Strings(params string[] values)
    {
        return new List<string>(values);
    }
}
