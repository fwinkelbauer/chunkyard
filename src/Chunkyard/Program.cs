namespace Chunkyard;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var parser = new CommandParser(
                new CheckCommandParser(),
                new CopyCommandParser(),
                new DiffCommandParser(),
                new GarbageCollectParser(),
                new KeepCommandParser(),
                new ListCommandParser(),
                new RemoveCommandParser(),
                new RestoreCommandParser(),
                new ShowCommandParser(),
                new StoreCommandParser());

            return parser.Parse(args).Run();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);

            return 1;
        }
    }
}
