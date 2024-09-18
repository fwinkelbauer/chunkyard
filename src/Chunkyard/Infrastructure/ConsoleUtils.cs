namespace Chunkyard.Infrastructure;

/// <summary>
/// A set of console related utility methods.
/// </summary>
internal static class ConsoleUtils
{
    public static void PrintDiff(DiffSet<Blob> diff)
    {
        foreach (var added in diff.Added)
        {
            Console.WriteLine($"+ {added.Name}");
        }

        foreach (var changed in diff.Changed)
        {
            Console.WriteLine($"~ {changed.Name}");
        }

        foreach (var removed in diff.Removed)
        {
            Console.WriteLine($"- {removed.Name}");
        }
    }
}
