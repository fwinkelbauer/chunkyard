namespace Chunkyard.Tests.CommandLine;

[TestClass]
public sealed class ArgsTests
{
    [TestMethod]
    public void Parse_Treats_Single_Argument_As_Command()
    {
        var expected = new Args("help", Some.Flags());

        var actual = Args.Parse("help");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Parse_Distinguishes_Between_Flags_And_Values()
    {
        var expected = new Args(
            "list",
            Some.Flags(
                ("--snapshot", Some.Strings("-2")),
                ("-f", Some.Strings("foo", "bar", "baz")),
                ("--dry-run", Some.Strings()),
                ("--force", Some.Strings())));

        var actual = Args.Parse(
            "list",
            "--snapshot", "-2",
            "-f", "foo", "bar",
            "-f", "baz",
            "--dry-run",
            "--force");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Parse_Accepts_Multi_Word_Command()
    {
        var expected = new Args(
            "command with spaces",
            Some.Flags(
                ("--help", Some.Strings())));

        var actual = Args.Parse("command", "with", "spaces", "--help");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Command_Can_Be_Empty()
    {
        var expected = new Args(
            "",
            Some.Flags(
                ("--help", Some.Strings())));

        var actual = Args.Parse("--help");

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Parse_Treats_Empty_Arguments_As_Error()
    {
        Assert.IsNull(Args.Parse());
    }
}
