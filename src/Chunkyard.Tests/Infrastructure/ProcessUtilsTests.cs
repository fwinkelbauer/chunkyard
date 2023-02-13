namespace Chunkyard.Tests.Infrastructure;

public static class ProcessUtilsTests
{
    [Fact]
    public static void Run_Throws_On_Invalid_Exit_Code()
    {
        Assert.Throws<InvalidOperationException>(
            () => ProcessUtils.Run("dotnet", "invalid-cmd"));

        Assert.Throws<InvalidOperationException>(
            () => ProcessUtils.Run("dotnet", "invalid-cmd", new[] { 0 }));
    }

    [Fact]
    public static void RunQuery_Throws_On_Invalid_Exit_Code()
    {
        Assert.Throws<InvalidOperationException>(
            () => ProcessUtils.RunQuery("dotnet", "invalid-cmd"));

        Assert.Throws<InvalidOperationException>(
            () => ProcessUtils.RunQuery("dotnet", "invalid-cmd", new[] { 0 }));
    }

    [Fact]
    public static void RunQuery_Returns_StandardOutput()
    {
        var output = ProcessUtils.RunQuery("dotnet", "--version");

        Assert.NotNull(Version.Parse(output));
    }
}
