namespace Chunkyard.Make;

public sealed class BuildCommandParser : ICommandParser
{
    public string Command => "build";

    public string Info => "Build the repository";

    public object Parse(FlagConsumer consumer)
    {
        return consumer.IsEmpty()
            ? new BuildCommand()
            : consumer.Help;
    }
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check for dependency updates";

    public object Parse(FlagConsumer consumer)
    {
        return consumer.IsEmpty()
            ? new CheckCommand()
            : consumer.Help;
    }
}

public sealed class CleanCommandParser : ICommandParser
{
    public string Command => "clean";

    public string Info => "Clean the repository";

    public object Parse(FlagConsumer consumer)
    {
        return consumer.IsEmpty()
            ? new CleanCommand()
            : consumer.Help;
    }
}

public sealed class FormatCommandParser : ICommandParser
{
    public string Command => "format";

    public string Info => "Run the formatter";

    public object Parse(FlagConsumer consumer)
    {
        return consumer.IsEmpty()
            ? new FormatCommand()
            : consumer.Help;
    }
}

public sealed class PublishCommandParser : ICommandParser
{
    public string Command => "publish";

    public string Info => "Publish the main project";

    public object Parse(FlagConsumer consumer)
    {
        return consumer.IsEmpty()
            ? new PublishCommand()
            : consumer.Help;
    }
}

public sealed class ReleaseCommandParser : ICommandParser
{
    public string Command => "release";

    public string Info => "Create a release commit";

    public object Parse(FlagConsumer consumer)
    {
        return consumer.IsEmpty()
            ? new ReleaseCommand()
            : consumer.Help;
    }
}

public sealed class BuildCommand
{
}

public sealed class CheckCommand
{
}

public sealed class CleanCommand
{
}

public sealed class FormatCommand
{
}

public sealed class PublishCommand
{
}

public sealed class ReleaseCommand
{
}
