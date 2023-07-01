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

            var command = parser.Parse(args);

            if (command is CatCommand cat)
                CommandHandler.Cat(cat);
            else if (command is CheckCommand check)
                CommandHandler.CheckSnapshot(check);
            else if (command is CopyCommand copy)
                CommandHandler.Copy(copy);
            else if (command is DiffCommand diff)
                CommandHandler.DiffSnapshots(diff);
            else if (command is GarbageCollectCommand gc)
                CommandHandler.GarbageCollect(gc);
            else if (command is KeepCommand keep)
                CommandHandler.KeepSnapshots(keep);
            else if (command is HelpCommand help)
            {
                Console.WriteLine(help.ToText());
                Environment.ExitCode = 1;
            }
            else if (command is ListCommand list)
                CommandHandler.ListSnapshots(list);
            else if (command is RemoveCommand remove)
                CommandHandler.RemoveSnapshot(remove);
            else if (command is RestoreCommand restore)
                CommandHandler.RestoreSnapshot(restore);
            else if (command is StoreCommand store)
                CommandHandler.StoreSnapshot(store);
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
