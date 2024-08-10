namespace Chunkyard.Tests.Cli;

[TestClass]
public sealed class CommandParserTests
{
    [TestMethod]
    public void Parse_Dispatches_To_Correct_Parser()
    {
        var parser = new CommandParser(
            new[]
            {
                new DummyCommandParser("one"),
                new DummyCommandParser("two"),
                new DummyCommandParser("three")
            });

        Assert.AreEqual("one", parser.Parse("one"));
        Assert.AreEqual("one", parser.Parse("one", "--help", "false"));
        Assert.AreEqual("two", parser.Parse("two"));
        Assert.AreEqual("three", parser.Parse("three"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("unknown")]
    [DataRow("cmd --unknown value")]
    [DataRow("cmd --help")]
    [DataRow("cmd --help true")]
    [DataRow("cmd --help invalid")]
    public void Parse_Returns_Help_For_Unknown_Invalid_Or_Help_Command(
        string args)
    {
        var parser = new CommandParser(
            new[]
            {
                new DummyCommandParser("cmd")
            });

        Assert.IsInstanceOfType<HelpCommand>(
            parser.Parse(args.Split(' ')));
    }
}
