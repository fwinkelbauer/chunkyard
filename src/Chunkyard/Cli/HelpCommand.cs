namespace Chunkyard.Cli;

/// <summary>
/// A class to encapsulate usage and error information when dealing with command
/// line arguments.
/// </summary>
public sealed class HelpCommand : ICommand
{
    public HelpCommand(
        IReadOnlyDictionary<string, string> infos,
        IReadOnlyCollection<string> errors)
    {
        Infos = infos;
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string> Infos { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public int Run()
    {
        Console.Error.WriteLine($"Chunkyard v{GetVersion()}");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  <command> <flags>");

        if (Infos.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Help:");

            foreach (var info in Infos.OrderBy(i => i.Key))
            {
                Console.Error.WriteLine($"  {info.Key}");
                Console.Error.WriteLine($"    {info.Value}");
            }
        }

        if (Errors.Any())
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

    private static string GetVersion()
    {
        var attribute = typeof(Program).Assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .First();

        return ((AssemblyInformationalVersionAttribute)attribute)
            .InformationalVersion;
    }
}
