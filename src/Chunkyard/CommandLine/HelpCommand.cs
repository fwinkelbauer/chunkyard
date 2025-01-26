namespace Chunkyard.CommandLine;

/// <summary>
/// A class to encapsulate usage and error information when dealing with command
/// line arguments.
/// </summary>
public sealed class HelpCommand : ICommand
{
    public HelpCommand(
        string headline,
        IReadOnlyDictionary<string, string> commandInfos,
        IReadOnlyDictionary<string, string> flagInfos,
        IReadOnlyCollection<string> errors)
    {
        Headline = headline;
        CommandInfos = commandInfos;
        FlagInfos = flagInfos;
        Errors = errors;
    }

    public string Headline { get; }

    public IReadOnlyDictionary<string, string> CommandInfos { get; }

    public IReadOnlyDictionary<string, string> FlagInfos { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public int Run()
    {
        Console.Error.WriteLine(Headline);

        WriteInfos("Commands:", CommandInfos);
        WriteInfos("Flags:", FlagInfos);

        if (Errors.Count > 0)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in Errors.OrderBy(e => e))
            {
                Console.Error.WriteLine($"  {error}");
            }
        }

        Console.Error.WriteLine();

        return 1;
    }

    private static void WriteInfos(
        string name,
        IReadOnlyDictionary<string, string> infos)
    {
        if (infos.Count > 0)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(name);

            foreach (var info in infos.OrderBy(i => i.Key))
            {
                Console.Error.WriteLine($"  {info.Key}");
                Console.Error.WriteLine($"    {info.Value}");
            }
        }
    }
}
