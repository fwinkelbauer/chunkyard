namespace Chunkyard;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            CommandParser.Parse(args)
                .Handle(new CommandHandler());
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
