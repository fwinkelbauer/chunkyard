namespace Chunkyard.Tests;

public static class ProgramTests
{
    [Theory]
    [InlineData("store --not-a-real-argument")]
    [InlineData("store")]
    [InlineData("store --help")]
    [InlineData("not-a-real-verb")]
    [InlineData("help")]
    [InlineData("")]
    public static void Invalid_Arguments_Or_Help_Commands_Return_ExitCode_One(
        string args)
    {
        Assert.Equal(1, Program.Main(args.Split(' ')));
    }
}
