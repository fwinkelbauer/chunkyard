namespace Chunkyard.Make;

public static class Program
{
    public static void Main(string[] args)
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

            if (command is BuildCommand)
                CommandHandler.Build();
            else if (command is CheckCommand)
                CommandHandler.Check();
            else if (command is CleanCommand)
                CommandHandler.Clean();
            else if (command is FormatCommand)
                CommandHandler.Format();
            else if (command is HelpCommand help)
                CommandHandler.Help(help);
            else if (command is PublishCommand)
                CommandHandler.Publish();
            else if (command is ReleaseCommand)
                CommandHandler.Release();
            else
                throw new NotImplementedException();
        }
        catch (Exception e)
        {
            CommandHandler.Error(e);
        }
    }
}
