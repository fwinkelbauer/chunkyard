namespace Chunkyard.Make;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var parser = new CommandParser(
                new SimpleCommandParser(
                    "build",
                    "Build the repository",
                    new BuildCommand()),
                new SimpleCommandParser(
                    "check",
                    "Check for dependency updates",
                    new CheckCommand()),
                new SimpleCommandParser(
                    "clean",
                    "Clean the repository",
                    new CleanCommand()),
                new SimpleCommandParser(
                    "format",
                    "Run the formatter",
                    new FormatCommand()),
                new SimpleCommandParser(
                    "publish",
                    "Publish the main project",
                    new PublishCommand()),
                new SimpleCommandParser(
                    "release",
                    "Create a release commit",
                    new ReleaseCommand()));

            var command = parser.Parse(args);

            Run<BuildCommand>(command, _ => CommandHandler.Build());
            Run<CheckCommand>(command, _ => CommandHandler.Check());
            Run<CleanCommand>(command, _ => CommandHandler.Clean());
            Run<FormatCommand>(command, _ => CommandHandler.Format());
            Run<HelpCommand>(command, CommandHandler.Help);
            Run<PublishCommand>(command, _ => CommandHandler.Publish());
            Run<ReleaseCommand>(command, _ => CommandHandler.Release());
        }
        catch (Exception e)
        {
            CommandHandler.Error(e);
        }

        return Environment.ExitCode;
    }

    private static void Run<T>(object obj, Action<T> handler)
    {
        if (obj is T t)
        {
            handler(t);
        }
    }
}
