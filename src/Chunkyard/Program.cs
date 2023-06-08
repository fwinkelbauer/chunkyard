namespace Chunkyard;

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

    private static void ProcessArguments(string[] args)
    {
        Parser.Default.ParseArguments<RestoreOptions, StoreOptions, CheckOptions, ShowOptions, RemoveOptions, KeepOptions, ListOptions, DiffOptions, GarbageCollectOptions, CopyOptions, CatOptions>(args)
            .WithParsed<RestoreOptions>(Commands.RestoreSnapshot)
            .WithParsed<StoreOptions>(Commands.StoreSnapshot)
            .WithParsed<CheckOptions>(Commands.CheckSnapshot)
            .WithParsed<ShowOptions>(Commands.ShowSnapshot)
            .WithParsed<KeepOptions>(Commands.KeepSnapshots)
            .WithParsed<ListOptions>(Commands.ListSnapshots)
            .WithParsed<DiffOptions>(Commands.DiffSnapshots)
            .WithParsed<GarbageCollectOptions>(Commands.GarbageCollect)
            .WithParsed<CopyOptions>(Commands.Copy)
            .WithParsed<CatOptions>(Commands.Cat)
            .WithParsed<RemoveOptions>(Commands.Remove)
            .WithNotParsed(_ => Environment.ExitCode = 1);
    }
}
