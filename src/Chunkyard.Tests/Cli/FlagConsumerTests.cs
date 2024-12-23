namespace Chunkyard.Tests.Cli;

[TestClass]
public sealed class FlagConsumerTests
{
    [TestMethod]
    public void TryStrings_Returns_Empty_List_On_Empty_Input()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryStrings(
            "--something",
            "info",
            out var list);

        Assert.IsTrue(success);
        CollectionAssert.Equals(0, list.Length);
    }

    [TestMethod]
    public void TryStrings_Returns_List_On_Non_Empty_Input()
    {
        var expected = Some.Strings("one", "two");

        var consumer = Some.FlagConsumer(
            ("--list", expected));

        var success = consumer.TryStrings("--list", "info", out var actual);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TryString_Returns_Nothing_On_Empty_Required_Input()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryString("--some", "info", out _);

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryString_Returns_Default_On_Empty_Optional_Input()
    {
        var expected = "default value";

        var consumer = Some.FlagConsumer();

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
        var consumer = Some.FlagConsumer(
            ("--value", Some.Strings("one", "two")));

        var success = consumer.TryString("--value", "info", out var actual);

        Assert.IsTrue(success);
        Assert.AreEqual("two", actual);
    }

    [TestMethod]
    public void TryBool_Returns_False_On_Empty_Input()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void TryBool_Returns_True_On_Empty_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings()));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void TryBool_Returns_False_On_Invalid_Input()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings("not-a-bool")));

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
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings(arg)));

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
        var consumer = Some.FlagConsumer(
            ("--enum", Some.Strings(arg)));

        var success = consumer.TryEnum<Day>("--enum", "info", out var actual);

        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void HelpNeeded_Returns_False_If_No_Issues()
    {
        var consumer = Some.FlagConsumer(
            ("--list", Some.Strings()));

        Assert.IsTrue(consumer.TryStrings("--list", "info", out _));
        Assert.IsFalse(consumer.HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_False_If_Not_Requested()
    {
        var consumer = Some.FlagConsumer(
            ("--help", Some.Strings("false")));

        Assert.IsFalse(consumer.HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_True_If_Requested()
    {
        Assert.IsTrue(Some.FlagConsumer(("--help", Some.Strings()))
            .HelpNeeded(out _));

        Assert.IsTrue(Some.FlagConsumer(("--help", Some.Strings("true")))
            .HelpNeeded(out _));

        Assert.IsTrue(Some.FlagConsumer(("--help", Some.Strings("not-a-bool")))
            .HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_True_On_Unconsumed_Arguments()
    {
        var consumer = Some.FlagConsumer(
            ("--list", Some.Strings("element")));

        Assert.IsTrue(consumer.HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_True_On_Invalid_Arguments()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings("not-a-bool")));

        Assert.IsFalse(consumer.TryBool("--bool", "info", out _));
        Assert.IsTrue(consumer.HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_True_On_Missing_Arguments()
    {
        var consumer = Some.FlagConsumer();

        Assert.IsFalse(consumer.TryString("--some", "info", out _));
        Assert.IsTrue(consumer.HelpNeeded(out _));
    }

    public enum Day
    {
        Monday = 0,
        Tuesday = 1
    }
}
