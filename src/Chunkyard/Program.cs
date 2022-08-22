namespace Chunkyard;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            DotEnv.Populate(Directory.GetCurrentDirectory());

            ProcessArguments(args);
        }
        catch (Exception e)
        {
            WriteError(e.Message);
        }
    }

    private static void WriteError(string message)
    {
        Environment.ExitCode = 1;

        try
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {message}");
        }
        finally
        {
            Console.ResetColor();
        }
    }

    private static void ProcessArguments(string[] args)
    {
        var optionTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
            .ToArray();

        Parser.Default.ParseArguments(args, optionTypes)
            .WithParsed<RestoreOptions>(Commands.RestoreSnapshot)
            .WithParsed<StoreOptions>(Commands.StoreSnapshot)
            .WithParsed<CheckOptions>(Commands.CheckSnapshot)
            .WithParsed<ShowOptions>(Commands.ShowSnapshot)
            .WithParsed<RemoveOptions>(Commands.RemoveSnapshot)
            .WithParsed<KeepOptions>(Commands.KeepSnapshots)
            .WithParsed<ListOptions>(Commands.ListSnapshots)
            .WithParsed<DiffOptions>(Commands.DiffSnapshots)
            .WithParsed<GarbageCollectOptions>(Commands.GarbageCollect)
            .WithParsed<CopyOptions>(Commands.Copy)
            .WithParsed<CatOptions>(Commands.Cat)
            .WithNotParsed(_ => Environment.ExitCode = 1);
    }
}
