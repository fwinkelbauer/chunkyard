namespace Chunkyard.Tests.Cli;

public static class FlagConsumerTests
{
    [Fact]
    public static void TryStrings_Returns_Empty_List_On_Empty_Input()
    {
        var consumer = Some.FlagConsumer();

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
        var expected = Some.Strings("one", "two");

        var consumer = Some.FlagConsumer(
            ("--list", expected));

        var success = consumer.TryStrings("--list", "info", out var actual);

        Assert.True(success);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void TryString_Returns_Nothing_On_Empty_Required_Input()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryString("--some", "info", out _);

        Assert.False(success);
    }

    [Fact]
    public static void TryString_Returns_Default_On_Empty_Optional_Input()
    {
        var expected = "default value";

        var consumer = Some.FlagConsumer();

        var success = consumer.TryString(
            "--some",
            "info",
            out var actual,
            expected);

        Assert.True(success);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void TryString_Returns_Last_String_On_Non_Empty_Input()
    {
        var consumer = Some.FlagConsumer(
            ("--value", Some.Strings("one", "two")));

        var success = consumer.TryString("--value", "info", out var actual);

        Assert.True(success);
        Assert.Equal("two", actual);
    }

    [Fact]
    public static void TryBool_Returns_False_On_Empty_Input()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.True(success);
        Assert.False(actual);
    }

    [Fact]
    public static void TryBool_Returns_True_On_Empty_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings()));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.True(success);
        Assert.True(actual);
    }

    [Fact]
    public static void TryBool_Returns_False_On_Invalid_Input()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings("not-a-bool")));

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
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings(arg)));

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
        var consumer = Some.FlagConsumer(
            ("--enum", Some.Strings(arg)));

        var success = consumer.TryEnum<Day>("--enum", "info", out var actual);

        Assert.True(success);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void NoHelp_Returns_True_If_No_Issues()
    {
        var consumer = Some.FlagConsumer(
            ("--list", Some.Strings()));

        Assert.True(consumer.TryStrings("--list", "info", out _));
        Assert.True(consumer.NoHelp(out _));
    }

    [Fact]
    public static void NoHelp_Returns_True_If_Not_Requested()
    {
        var consumer = Some.FlagConsumer(
            ("--help", Some.Strings("false")));

        Assert.True(consumer.NoHelp(out _));
    }

    [Fact]
    public static void NoHelp_Returns_False_If_Requested()
    {
        Assert.False(Some.FlagConsumer(("--help", Some.Strings()))
            .NoHelp(out _));

        Assert.False(Some.FlagConsumer(("--help", Some.Strings("true")))
            .NoHelp(out _));

        Assert.False(Some.FlagConsumer(("--help", Some.Strings("not-a-bool")))
            .NoHelp(out _));
    }

    [Fact]
    public static void NoHelp_Returns_False_On_Unconsumed_Arguments()
    {
        var consumer = Some.FlagConsumer(
            ("--list", Some.Strings("element")));

        Assert.False(consumer.NoHelp(out _));
    }

    [Fact]
    public static void NoHelp_Returns_False_On_Invalid_Arguments()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings("not-a-bool")));

        Assert.False(consumer.TryBool("--bool", "info", out _));
        Assert.False(consumer.NoHelp(out _));
    }

    [Fact]
    public static void NoHelp_Returns_False_On_Missing_Arguments()
    {
        var consumer = Some.FlagConsumer();

        Assert.False(consumer.TryString("--some", "info", out _));
        Assert.False(consumer.NoHelp(out _));
    }

    public enum Day
    {
        Monday = 0,
        Tuesday = 1
    }
}
