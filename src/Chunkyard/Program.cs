namespace Chunkyard;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            var parser = new CommandParser(
                new CatCommandParser(),
                new CheckCommandParser(),
                new CopyCommandParser(),
                new DiffCommandParser(),
                new GarbageCollectCommandParser(),
                new KeepCommandParser(),
                new ListCommandParser(),
                new RemoveCommandParser(),
                new RestoreCommandParser(),
                new ShowCommandParser(),
                new StoreCommandParser());

            parser.Parse(args).Run();
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
