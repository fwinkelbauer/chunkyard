namespace Chunkyard.Make;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Directory.SetCurrentDirectory(
                CommandUtils.GitQuery("rev-parse --show-toplevel"));

            Environment.SetEnvironmentVariable(
                "DOTNET_CLI_TELEMETRY_OPTOUT",
                "1");

            var parser = new CommandParser(
                new BuildCommandParser(),
                new CheckCommandParser(),
                new CleanCommandParser(),
                new FormatCommandParser(),
                new PublishCommandParser(),
                new ReleaseCommandParser());

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
