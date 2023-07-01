namespace Chunkyard.Make;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var parser = new CommandParser(
                new BuildCommandParser(),
                new CheckCommandParser(),
                new CleanCommandParser(),
                new FormatCommandParser(),
                new PublishCommandParser(),
                new ReleaseCommandParser());

            var command = parser.Parse(args);

            if (command is BuildCommand)
                CommandHandler.Build();
            else if (command is CheckCommand)
                CommandHandler.Check();
            else if (command is CleanCommand)
                CommandHandler.Clean();
            else if (command is FormatCommand)
                CommandHandler.Format();
            else if (command is PublishCommand)
                CommandHandler.Publish();
            else if (command is HelpCommand help)
            {
                Console.WriteLine(help.ToText());
                Environment.ExitCode = 1;
            }
            else if (command is ReleaseCommand)
                CommandHandler.Release();
            else
                throw new NotImplementedException();
        }
        catch (Exception e)
        {
            PrintError(e);
            Environment.ExitCode = 1;
        }
    }

    private static void PrintError(Exception e)
    {
        Console.Error.WriteLine("Error:");

        IReadOnlyCollection<Exception> exceptions = e is AggregateException a
            ? a.InnerExceptions
            : new[] { e };

        var debugMode = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("CHUNKYARD_DEBUG"));

        foreach (var exception in exceptions)
        {
            Console.Error.WriteLine(debugMode
                ? exception.ToString()
                : exception.Message);
        }
    }
}
