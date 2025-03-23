[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
namespace Chunkyard.Tests;

[TestClass]
public sealed class ProgramTests
{
    [TestMethod]
    [DataRow("store --not-a-real-argument")]
    [DataRow("store")]
    [DataRow("store --help")]
    [DataRow("invalid-cmd")]
    [DataRow("help")]
    [DataRow("")]
    public void Invalid_Arguments_Or_Help_Commands_Return_ExitCode_One(
        string args)
    {
        Assert.AreEqual(1, Program.Main(args.Split(' ')));
    }
}
