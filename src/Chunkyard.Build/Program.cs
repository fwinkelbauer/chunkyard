namespace Chunkyard.Build;

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

            Handle<BuildCommand>(command, _ => CommandHandler.Build());
            Handle<CheckCommand>(command, _ => CommandHandler.Check());
            Handle<CleanCommand>(command, _ => CommandHandler.Clean());
            Handle<FormatCommand>(command, _ => CommandHandler.Format());
            Handle<PublishCommand>(command, _ => CommandHandler.Publish());
            Handle<ReleaseCommand>(command, _ => CommandHandler.Release());

            Handle<HelpCommand>(command, DefaultCommandHandler.Help);
        }
        catch (Exception e)
        {
            DefaultCommandHandler.Error(e);
        }

        return Environment.ExitCode;
    }

    private static void Handle<T>(object obj, Action<T> handler)
    {
        if (obj is T t)
        {
            handler(t);
        }
    }
}
