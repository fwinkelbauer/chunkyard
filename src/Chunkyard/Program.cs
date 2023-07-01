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
                CommandHandler.Check(check);
            else if (command is CopyCommand copy)
                CommandHandler.Copy(copy);
            else if (command is DiffCommand diff)
                CommandHandler.Diff(diff);
            else if (command is GarbageCollectCommand gc)
                CommandHandler.GarbageCollect(gc);
            else if (command is KeepCommand keep)
                CommandHandler.Keep(keep);
            else if (command is HelpCommand help)
                CommandHandler.Help(help);
            else if (command is ListCommand list)
                CommandHandler.List(list);
            else if (command is RemoveCommand remove)
                CommandHandler.Remove(remove);
            else if (command is RestoreCommand restore)
                CommandHandler.Restore(restore);
            else if (command is StoreCommand store)
                CommandHandler.Store(store);
            else
                throw new NotImplementedException();
        }
        catch (Exception e)
        {
            CommandHandler.Error(e);
        }
    }
}
