namespace Chunkyard.Make;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            ProcessArguments(args);
        }
        catch (Exception e)
        {
            PrintError(e);
            Environment.ExitCode = 1;
        }

        return Environment.ExitCode;
    }

    private static void PrintError(Exception e)
    {
        Console.Error.WriteLine("Error:");

        if (!string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("CHUNKYARD_DEBUG")))
        {
            Console.Error.WriteLine(e.ToString());
        }
        else
        {
            IReadOnlyCollection<Exception> exceptions = e is AggregateException a
                ? a.InnerExceptions
                : new[] { e };

            foreach (var exception in exceptions)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }
    }

    private static void ProcessArguments(string[] args)
    {
        Parser.Default.ParseArguments<CleanOptions, BuildOptions, PublishOptions, ReleaseOptions, FormatOptions, CheckOptions>(args)
            .WithParsed<CleanOptions>(_ => Commands.Clean())
            .WithParsed<BuildOptions>(_ => Commands.Build())
            .WithParsed<PublishOptions>(_ => Commands.Publish())
            .WithParsed<ReleaseOptions>(_ => Commands.Release())
            .WithParsed<FormatOptions>(_ => Commands.Format())
            .WithParsed<CheckOptions>(_ => Commands.Check())
            .WithNotParsed(_ => Environment.ExitCode = 1);
    }
}
