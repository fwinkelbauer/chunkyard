namespace Chunkyard.Tests.Cli;

public static class ArgTests
{
    [Fact]
    public static void Parse_Treats_Single_Argument_As_Command()
    {
        var expected = Result.Success(
            new Arg("help", Some.Dict<string, IReadOnlyCollection<string>>()));

        var actual = Arg.Parse("help");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Parse_Distinguishes_Between_Flags_And_Values()
    {
        var expected = Result.Success(
            new Arg(
                "list",
                Some.Dict(
                    ("--snapshot", Some.Strings("-2")),
                    ("-f", Some.Strings("foo", "bar", "baz")),
                    ("--preview", Some.Strings()),
                    ("--force", Some.Strings()))));

        var actual = Arg.Parse(
            "list",
            "--snapshot", "-2",
            "-f", "foo", "bar",
            "-f", "baz",
            "--preview",
            "--force");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Parse_Must_Have_Flag_To_Accept_Values()
    {
        var expected = Result.Error<Arg>(
            "Unexpected value without a flag: bad-value");

        var actual = Arg.Parse("help", "bad-value", "--version");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void Parse_Treats_Empty_Arguments_As_Error()
    {
        var expected = Result.Error<Arg>(
            "No arguments provided");

        var actual = Arg.Parse();

        Assert.Equal(expected, actual);
    }
}
