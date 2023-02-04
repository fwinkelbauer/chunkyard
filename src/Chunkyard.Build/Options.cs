namespace Chunkyard.Build;

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

[Verb("fmt", HelpText = "Run the formatter.")]
public class FmtOptions
{
}

[Verb("outdated", HelpText = "Search for outdated dependencies.")]
public class OutdatedOptions
{
}

[Verb("release", HelpText = "Create a release commit.")]
public class ReleaseOptions
{
}
