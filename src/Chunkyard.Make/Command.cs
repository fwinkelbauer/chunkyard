namespace Chunkyard.Make;

public sealed class BuildCommandParser : ICommandParser
{
    public string Command => "build";

    public string Info => "Build the repository";

    public object Parse(FlagConsumer consumer) => new BuildCommand();
}

public sealed class CheckCommandParser : ICommandParser
{
    public string Command => "check";

    public string Info => "Check for dependency updates";

    public object Parse(FlagConsumer consumer) => new CheckCommand();
}

public sealed class CleanCommandParser : ICommandParser
{
    public string Command => "clean";

    public string Info => "Clean the repository";

    public object Parse(FlagConsumer consumer) => new CleanCommand();
}

public sealed class FormatCommandParser : ICommandParser
{
    public string Command => "format";

    public string Info => "Run the formatter";

    public object Parse(FlagConsumer consumer) => new FormatCommand();
}

public sealed class PublishCommandParser : ICommandParser
{
    public string Command => "publish";

    public string Info => "Publish the main project";

    public object Parse(FlagConsumer consumer) => new PublishCommand();
}

public sealed class ReleaseCommandParser : ICommandParser
{
    public string Command => "release";

    public string Info => "Create a release commit";

    public object Parse(FlagConsumer consumer) => new ReleaseCommand();
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
