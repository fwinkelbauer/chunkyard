namespace Chunkyard.Cli.Tests;

internal static class Some
{
    public static Dictionary<string, IReadOnlyCollection<string>> Flags(
        params (string Key, IReadOnlyCollection<string> Value)[] pairs)
    {
        return pairs.ToDictionary(p => p.Key, p => p.Value);
    }

    public static IReadOnlyCollection<string> Strings(params string[] values)
    {
        return new List<string>(values);
    }
}
