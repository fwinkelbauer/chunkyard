namespace Chunkyard.Make;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return ProcessArguments(args);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error: {e.Message}");
            return 1;
        }
    }

    private static int ProcessArguments(string[] args)
    {
        var rootCommand = new RootCommand("A tool to build and publish Chunkyard");

        void Command(string name, string description, Action handler)
        {
            var command = new Command(name, description);
            command.SetHandler(handler);
            rootCommand.Add(command);
        }

        Command("clean", "Clean the repository", Commands.Clean);
        Command("build", "Build the repository", Commands.Build);
        Command("publish", "Publish the main project", Commands.Publish);
        Command("format", "Run the formatter", Commands.Format);
        Command("outdated", "Search for outdated dependencies", Commands.Outdated);
        Command("release", "Create a release commit", Commands.Release);

        return rootCommand.Invoke(args);
    }
}
