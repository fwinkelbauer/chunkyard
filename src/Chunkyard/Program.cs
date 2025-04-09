namespace Chunkyard;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var parser = new CommandParser()
                .With("check", "Check if a snapshot is valid", CheckCommand.Parse)
                .With("copy", "Copy snapshots from one repository to another", CopyCommand.Parse)
                .With("diff", "Show the difference between two snapshots", DiffCommand.Parse)
                .With("gc", "Remove unreferenced chunks", GarbageCollectCommand.Parse)
                .With("keep", "Keep only a certain amount of snapshots", KeepCommand.Parse)
                .With("list", "List all snapshots", ListCommand.Parse)
                .With("remove", "Remove a snapshot", RemoveCommand.Parse)
                .With("restore", "Restore a snapshot", RestoreCommand.Parse)
                .With("show", "Show the content of a snapshot", ShowCommand.Parse)
                .With("store", "Store a new snapshot", StoreCommand.Parse);

            return parser.Parse(args).Run();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return 1;
        }
    }
}
