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
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  <command> <flags>");

        if (c.HelpTexts.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Help:");

            foreach (var helpText in c.HelpTexts)
            {
                Console.Error.WriteLine($"  {helpText.Topic}");
                Console.Error.WriteLine($"    {helpText.Info}");
            }
        }

        if (c.Errors.Any())
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(c.Errors.Count == 1 ? "Error:" : "Errors:");

            foreach (var error in c.Errors)
            {
                Console.Error.WriteLine($"  {error}");
            }
        }

        Console.Error.WriteLine();

        Environment.ExitCode = 1;
    }
}
