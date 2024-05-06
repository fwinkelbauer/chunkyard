namespace Chunkyard.Cli.Tests;

public static class FlagConsumerTests
{
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

        var consumer = new FlagConsumer(
            Some.Dict(("--list", expectedList)));

        var success = consumer.TryStrings("--list", "info", out var actualList);

        Assert.True(success);
        Assert.Equal(expectedList, actualList);
    }

    [Fact]
    public static void TryString_Returns_Nothing_On_Empty_Required_Input()
    {
        var consumer = new FlagConsumer(
            Some.Dict<string, IReadOnlyCollection<string>>());

        var success = consumer.TryString("--some", "info", out _);

        Assert.False(success);
    }

    [Fact]
    public static void TryString_Returns_Default_On_Empty_Optional_Input()
    {
        var expectedValue = "default value";

        var consumer = new FlagConsumer(
            Some.Dict<string, IReadOnlyCollection<string>>());

        var success = consumer.TryString(
            "--some",
            "info",
            out var actual,
            expectedValue);

        Assert.True(success);
        Assert.Equal(expectedValue, actual);
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
        var consumer = new FlagConsumer(
            Some.Dict(("--bool", Some.Strings())));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.True(success);
        Assert.True(actual);
    }

    [Fact]
    public static void TryBool_Returns_False_On_Invalid_Input()
    {
        var consumer = new FlagConsumer(
            Some.Dict(("--bool", Some.Strings("not-a-bool"))));

        var success = consumer.TryBool("--bool", "info", out _);

        Assert.False(success);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TrUe", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public static void TryBool_Ignores_Case(string arg, bool expected)
    {
        var consumer = new FlagConsumer(
            Some.Dict(("--bool", Some.Strings(arg))));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.True(success);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Monday", Day.Monday)]
    [InlineData("monday", Day.Monday)]
    [InlineData("TUESDAY", Day.Tuesday)]
    public static void TryEnum_Ignores_Case(string arg, Day expected)
    {
        var consumer = new FlagConsumer(
            Some.Dict(("--enum", Some.Strings(arg))));

        var success = consumer.TryEnum<Day>("--enum", "info", out var actual);

        Assert.True(success);
        Assert.Equal(expected, actual);
    }

    public enum Day
    {
        Monday = 0,
        Tuesday = 1
    }
}
