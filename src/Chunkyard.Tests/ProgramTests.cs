namespace Chunkyard.Tests;

public class ProgramTests
{
    private readonly ITestOutputHelper _output;

    public ProgramTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown-verb")]
    public void Chunkyard_Throws_Given_No_Or_Unknown_Verb(string verb)
    {
        Assert.Throws<InvalidOperationException>(
            () => Chunkyard(verb));
    }

    [Fact]
    public void Chunkyard_Runs_Verb()
    {
        using var directory = new DisposableDirectory();

        Chunkyard($"list -r {directory.Name}");
    }

    private void Chunkyard(string arguments)
    {
        var startInfo = new ProcessStartInfo(
            "dotnet",
            $"run --project src/Chunkyard -- {arguments}")
        {
            WorkingDirectory = ProcessUtils.RunQuery(
                "git",
                "rev-parse --show-toplevel"),
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        ProcessUtils.Run(
            startInfo,
            new[] { 0 },
            _output.WriteLine);
    }
}
