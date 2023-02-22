namespace Chunkyard.Tests;

public static class ProgramTests
{
    [Theory]
    [InlineData("store --not-a-real-argument")]
    [InlineData("store")]
    [InlineData("not-a-real-verb")]
    [InlineData("")]
    [InlineData("version")]
    [InlineData("help")]
    public static void Invalid_Arguments_Or_Helper_Verbs_Return_ExitCode_One(string args)
    {
        Assert.Equal(1, Program.Main(args.Split(' ')));
    }
}
