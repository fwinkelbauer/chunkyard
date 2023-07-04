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

            Handle<CatCommand>(command, CommandHandler.Cat);
            Handle<CheckCommand>(command, CommandHandler.Check);
            Handle<CopyCommand>(command, CommandHandler.Copy);
            Handle<DiffCommand>(command, CommandHandler.Diff);
            Handle<GarbageCollectCommand>(command, CommandHandler.GarbageCollect);
            Handle<KeepCommand>(command, CommandHandler.Keep);
            Handle<HelpCommand>(command, CommandHandler.Help);
            Handle<ListCommand>(command, CommandHandler.List);
            Handle<RemoveCommand>(command, CommandHandler.Remove);
            Handle<RestoreCommand>(command, CommandHandler.Restore);
            Handle<ShowCommand>(command, CommandHandler.Show);
            Handle<StoreCommand>(command, CommandHandler.Store);
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

    private static void Handle<T>(object obj, Action<T> handler)
    {
        if (obj is T t)
        {
            handler(t);
        }
    }
}
