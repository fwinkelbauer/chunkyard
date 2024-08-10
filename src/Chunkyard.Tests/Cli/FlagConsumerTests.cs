namespace Chunkyard.Tests.Cli;

[TestClass]
public sealed class FlagConsumerTests
{
    [TestMethod]
    public void TryStrings_Returns_Empty_List_On_Empty_Input()
    {
        var consumer = new FlagConsumer(
            Some.Flags());

        var success = consumer.TryStrings(
            "--something",
            "info",
            out var list);

        Assert.IsTrue(success);
        Assert.IsFalse(list.Any());
    }

    [TestMethod]
    public void TryStrings_Returns_List_On_Non_Empty_Input()
    {
        var expected = Some.Strings("one", "two");

        var consumer = new FlagConsumer(
            Some.Flags(("--list", expected)));

        var success = consumer.TryStrings("--list", "info", out var actual);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TryString_Returns_Nothing_On_Empty_Required_Input()
    {
        var consumer = new FlagConsumer(
            Some.Flags());

        var success = consumer.TryString("--some", "info", out _);

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryString_Returns_Default_On_Empty_Optional_Input()
    {
        var expected = "default value";

        var consumer = new FlagConsumer(
            Some.Flags());

        var success = consumer.TryString(
            "--some",
            "info",
            out var actual,
            expected);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TryString_Returns_Last_String_On_Non_Empty_Input()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--value", Some.Strings("one", "two"))));

        var success = consumer.TryString("--value", "info", out var actual);

        Assert.IsTrue(success);
        Assert.AreEqual("two", actual);
    }

    [TestMethod]
    public void TryBool_Returns_False_On_Empty_Input()
    {
        var consumer = new FlagConsumer(
            Some.Flags());

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void TryBool_Returns_True_On_Empty_Flag()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--bool", Some.Strings())));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void TryBool_Returns_False_On_Invalid_Input()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--bool", Some.Strings("not-a-bool"))));

        var success = consumer.TryBool("--bool", "info", out _);

        Assert.IsFalse(success);
    }

    [TestMethod]
    [DataRow("true", true)]
    [DataRow("TrUe", true)]
    [DataRow("false", false)]
    [DataRow("FALSE", false)]
    public void TryBool_Ignores_Case(string arg, bool expected)
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--bool", Some.Strings(arg))));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("Monday", Day.Monday)]
    [DataRow("monday", Day.Monday)]
    [DataRow("TUESDAY", Day.Tuesday)]
    public void TryEnum_Ignores_Case(string arg, Day expected)
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--enum", Some.Strings(arg))));

        var success = consumer.TryEnum<Day>("--enum", "info", out var actual);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void NoHelp_Returns_True_If_No_Issues()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--list", Some.Strings())));

        Assert.IsTrue(consumer.TryStrings("--list", "info", out _));
        Assert.IsTrue(consumer.NoHelp(out _));
    }

    [TestMethod]
    public void NoHelp_Returns_True_If_Not_Requested()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--help", Some.Strings("false"))));

        Assert.IsTrue(consumer.NoHelp(out _));
    }

    [TestMethod]
    public void NoHelp_Returns_False_If_Requested()
    {
        Assert.IsFalse(new FlagConsumer(Some.Flags(("--help", Some.Strings())))
            .NoHelp(out _));

        Assert.IsFalse(new FlagConsumer(Some.Flags(("--help", Some.Strings("true"))))
            .NoHelp(out _));

        Assert.IsFalse(new FlagConsumer(Some.Flags(("--help", Some.Strings("not-a-bool"))))
            .NoHelp(out _));
    }

    [TestMethod]
    public void NoHelp_Returns_False_On_Unconsumed_Arguments()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--list", Some.Strings("element"))));

        Assert.IsFalse(consumer.NoHelp(out _));
    }

    [TestMethod]
    public void NoHelp_Returns_False_On_Invalid_Arguments()
    {
        var consumer = new FlagConsumer(
            Some.Flags(("--bool", Some.Strings("not-a-bool"))));

        Assert.IsFalse(consumer.TryBool("--bool", "info", out _));
        Assert.IsFalse(consumer.NoHelp(out _));
    }

    [TestMethod]
    public void NoHelp_Returns_False_On_Missing_Arguments()
    {
        var consumer = new FlagConsumer(Some.Flags());

        Assert.IsFalse(consumer.TryString("--some", "info", out _));
        Assert.IsFalse(consumer.NoHelp(out _));
    }

    public enum Day
    {
        Monday = 0,
        Tuesday = 1
    }
}
