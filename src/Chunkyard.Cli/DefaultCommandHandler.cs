namespace Chunkyard.Cli;

/// <summary>
/// A default handler for common commands.
/// </summary>
public static class DefaultCommandHandler
{
    public static void Error(Exception e)
    {
        Console.Error.WriteLine("Error:");

        IReadOnlyCollection<Exception> exceptions = e is AggregateException a
            ? a.InnerExceptions
            : new[] { e };

        var debugMode = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("DEBUG"));

        foreach (var exception in exceptions)
        {
            Console.Error.WriteLine(debugMode
                ? exception.ToString()
                : exception.Message);
        }

        Environment.ExitCode = 1;
    }

    public static void Help(HelpCommand c)
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  <command> <flags>");
        Console.WriteLine("  <command> --help");
        Console.WriteLine("  help");

        if (c.HelpTexts.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Help:");

            foreach (var helpText in c.HelpTexts)
            {
                Console.WriteLine($"  {helpText.Topic}");
                Console.WriteLine($"    {helpText.Info}");
            }
        }

        if (c.Errors.Any())
        {
            Console.WriteLine();
            Console.WriteLine(c.Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in c.Errors)
            {
                Console.WriteLine($"  {error}");
            }
        }

        Console.WriteLine();

        Environment.ExitCode = 1;
    }
}
