namespace Chunkyard;

public static class Program
{
    public static int Main(string[] args)
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

            var command = parser.Parse(
                PopulateArguments(args));

            Run<CatCommand>(command, CommandHandler.Cat);
            Run<CheckCommand>(command, CommandHandler.Check);
            Run<CopyCommand>(command, CommandHandler.Copy);
            Run<DiffCommand>(command, CommandHandler.Diff);
            Run<GarbageCollectCommand>(command, CommandHandler.GarbageCollect);
            Run<KeepCommand>(command, CommandHandler.Keep);
            Run<HelpCommand>(command, CommandHandler.Help);
            Run<ListCommand>(command, CommandHandler.List);
            Run<RemoveCommand>(command, CommandHandler.Remove);
            Run<RestoreCommand>(command, CommandHandler.Restore);
            Run<ShowCommand>(command, CommandHandler.Show);
            Run<StoreCommand>(command, CommandHandler.Store);
        }
        catch (Exception e)
        {
            CommandHandler.Error(e);
        }

        return Environment.ExitCode;
    }

    private static string[] PopulateArguments(string[] args)
    {
        const string config = ".chunkyard.config";

        if (args.Length > 0 && File.Exists(config))
        {
            var mergedArgs = new List<string>
            {
                args[0]
            };

            mergedArgs.AddRange(File.ReadAllText(config).Split(' '));
            mergedArgs.AddRange(args[1..]);

            return mergedArgs.ToArray();
        }
        else
        {
            return args;
        }
    }

    private static void Run<T>(object obj, Action<T> handler)
    {
        if (obj is T t)
        {
            handler(t);
        }
    }
}
