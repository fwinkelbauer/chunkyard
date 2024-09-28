namespace Chunkyard.Tests.Cli;

public static class ArgsTests
{
    [Fact]
    public static void Parse_Treats_Single_Argument_As_Command()
    {
        var expected = new Args("help", Some.Flags());

        var actual = Args.Parse("help");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Parse_Distinguishes_Between_Flags_And_Values()
    {
        var expected = new Args(
            "list",
            Some.Flags(
                ("--snapshot", Some.Strings("-2")),
                ("-f", Some.Strings("foo", "bar", "baz")),
                ("--preview", Some.Strings()),
                ("--force", Some.Strings())));

        var actual = Args.Parse(
            "list",
            "--snapshot", "-2",
            "-f", "foo", "bar",
            "-f", "baz",
            "--preview",
            "--force");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Parse_Accepts_Multi_Word_Command()
    {
        var expected = new Args(
            "command with spaces",
            Some.Flags(
                ("--help", Some.Strings())));

        var actual = Args.Parse("command", "with", "spaces", "--help");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Parse_Treats_Empty_Arguments_As_Error()
    {
        Assert.Null(Args.Parse());
    }
}
