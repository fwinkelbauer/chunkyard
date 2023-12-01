namespace Chunkyard.Cli.Tests;

internal static class Some
{
    public static Dictionary<TKey, TValue> Dict<TKey, TValue>(
        params (TKey Key, TValue Value)[] pairs)
        where TKey : notnull
    {
        return pairs.ToDictionary(p => p.Key, p => p.Value);
    }

    public static IReadOnlyCollection<string> Strings(params string[] values)
    {
        return new List<string>(values);
    }
}
