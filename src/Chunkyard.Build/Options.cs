namespace Chunkyard.Build;

[Verb("build", HelpText = "Build the solution.")]
public class BuildOptions
{
}

[Verb("ci", HelpText = "Build and test the solution.")]
public class CiOptions
{
}

[Verb("clean", HelpText = "Clean the solution.")]
public class CleanOptions
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

[Verb("publish", HelpText = "Publish the main project.")]
public class PublishOptions
{
}

[Verb("release", HelpText = "Create a release commit.")]
public class ReleaseOptions
{
}

[Verb("test", HelpText = "Test the solution.")]
public class TestOptions
{
    public TestOptions(bool verbose)
    {
        Verbose = verbose;
    }

    [Option("verbose", Required = false, HelpText = "Print more details.", Default = false)]
    public bool Verbose { get; }
}
