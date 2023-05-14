namespace Chunkyard.Make;

[Verb("clean", HelpText = "Clean the repository.")]
public class CleanOptions
{
}

[Verb("build", HelpText = "Build the repository.")]
public class BuildOptions
{
}

[Verb("publish", HelpText = "Publish the main project.")]
public class PublishOptions
{
}

[Verb("format", HelpText = "Run the formatter.")]
public class FormatOptions
{
}

[Verb("check", HelpText = "Check dependencies.")]
public class CheckOptions
{
}

[Verb("release", HelpText = "Create a release commit.")]
public class ReleaseOptions
{
}
