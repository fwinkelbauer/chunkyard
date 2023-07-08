namespace Chunkyard.Cli.Tests;

public static class FlagConsumerTests
{
    [Fact]
    public static void TryEmpty_Clears_Consumer()
    {
        var consumer = new FlagConsumer(
            Some.Dict(("--some", Some.Strings("value"))));

        Assert.False(consumer.TryEmpty());
        Assert.True(consumer.TryEmpty());
    }

    [Fact]
    public static void TryStrings_Returns_Empty_List_On_Empty_Input()
    {
        var consumer = new FlagConsumer(
            Some.Dict<string, IReadOnlyCollection<string>>());

        var success = consumer.TryStrings(
            "--something",
            "info",
            out var list);

        Assert.True(success);
        Assert.Empty(list);
    }

    [Fact]
    public static void TryStrings_Returns_List_On_Non_Empty_Input()
    {
        var expectedList = Some.Strings("one", "two");

        var expectedHelp = new HelpCommand(
            new[] { new HelpText("--list", "info") },
            Array.Empty<string>());

        var consumer = new FlagConsumer(
            Some.Dict(("--list", expectedList)));

        var success = consumer.TryStrings("--list", "info", out var actualList);

        Assert.True(success);
        Assert.Equal(expectedList, actualList);
        Assert.Equal(expectedHelp, consumer.Help);
    }

    [Fact]
    public static void TryString_Returns_Nothing_On_Empty_Required_Input()
    {
        var expectedHelp = new HelpCommand(
            new[] { new HelpText("--some", "info") },
            new[] { "Missing mandatory flag: --some" });

        var consumer = new FlagConsumer(
            Some.Dict<string, IReadOnlyCollection<string>>());

        var success = consumer.TryString("--some", "info", out _);

        Assert.False(success);
        Assert.Equal(expectedHelp, consumer.Help);
    }

    [Fact]
    public static void TryString_Returns_Default_On_Empty_Optional_Input()
    {
        var expectedValue = "default value";

        var expectedHelp = new HelpCommand(
            new[] { new HelpText("--some", $"info. Default: {expectedValue}") },
            Array.Empty<string>());

        var consumer = new FlagConsumer(
            Some.Dict<string, IReadOnlyCollection<string>>());

        var success = consumer.TryString(
            "--some",
            "info",
            out var actual,
            expectedValue);

        Assert.True(success);
        Assert.Equal(expectedValue, actual);
        Assert.Equal(expectedHelp, consumer.Help);
    }

    [Fact]
    public static void TryString_Returns_Last_String_On_Non_Empty_Input()
    {
        var consumer = new FlagConsumer(
            Some.Dict(("--value", Some.Strings("one", "two"))));

        var success = consumer.TryString("--value", "info", out var actual);

        Assert.True(success);
        Assert.Equal("two", actual);
    }

    [Fact]
    public static void TryBool_Returns_False_On_Empty_Input()
    {
        var consumer = new FlagConsumer(
            Some.Dict<string, IReadOnlyCollection<string>>());

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.True(success);
        Assert.False(actual);
    }

    [Fact]
    public static void TryBool_Returns_True_On_Empty_Flag()
    {
        var expectedHelp = new HelpCommand(
            new[] { new HelpText("--bool", "info") },
            Array.Empty<string>());

        var consumer = new FlagConsumer(
            Some.Dict(("--bool", Some.Strings())));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.True(success);
        Assert.True(actual);
        Assert.Equal(expectedHelp, consumer.Help);
    }

    [Fact]
    public static void TryBool_Returns_False_On_Invalid_Input()
    {
        var expectedHelp = new HelpCommand(
            new[] { new HelpText("--bool", "info. Default: False") },
            new[] { "Invalid value: --bool" });

        var consumer = new FlagConsumer(
            Some.Dict(("--bool", Some.Strings("not-a-bool"))));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.False(success);
        Assert.Equal(expectedHelp, consumer.Help);
    }
}
