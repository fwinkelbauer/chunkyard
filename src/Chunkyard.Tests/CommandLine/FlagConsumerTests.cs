namespace Chunkyard.Tests.CommandLine;

[TestClass]
public sealed class FlagConsumerTests
{
    [TestMethod]
    public void TryStrings_Returns_Nothing_For_Missing_Flag()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryStrings("--some", "info", out var list);

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryStrings_Returns_Empty_List_For_Empty_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--some", Some.Strings()));

        var success = consumer.TryStrings("--some", "info", out var list);

        Assert.IsTrue(success);
        Assert.AreEqual(0, list.Length);
    }

    [TestMethod]
    public void TryStrings_Returns_List_For_Non_Empty_Flag()
    {
        var expected = Some.Strings("one", "two");

        var consumer = Some.FlagConsumer(
            ("--list", expected));

        var success = consumer.TryStrings("--list", "info", out var actual);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TryStrings_Returns_Default_For_Missing_Optional_Flag()
    {
        var expected = Some.Strings("one", "two");

        var consumer = Some.FlagConsumer();

        var success = consumer.TryStrings(
            "--list",
            "info",
            out var actual,
            expected);

        Assert.IsTrue(success);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void TryStrings_Returns_Same_Value_When_Asked_Again()
    {
        var expected = Some.Strings("one", "two");

        var consumer = Some.FlagConsumer(
            ("--list", expected));

        Assert.IsTrue(consumer.TryStrings("--list", "info", out var actual1));
        CollectionAssert.AreEqual(expected, actual1);

        Assert.IsTrue(consumer.TryStrings("--list", "info", out var actual2));
        CollectionAssert.AreEqual(expected, actual2);
    }

    [TestMethod]
    public void TryString_Returns_Nothing_For_Missing_Flag()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryString("--some", "info", out _);

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryString_Returns_Default_For_Missing_Optional_Flag()
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
    public void TryString_Returns_Nothing_For_Empty_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--some", Some.Strings()));

        var success = consumer.TryString(
            "--some",
            "info",
            out _,
            "default value");

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryString_Returns_Last_String_For_Non_Empty_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--some", Some.Strings("one", "two")));

        var success = consumer.TryString("--some", "info", out var actual);

        Assert.IsTrue(success);
        Assert.AreEqual("two", actual);
    }

    [TestMethod]
    public void TryBool_Returns_False_For_Missing_Flag()
    {
        var consumer = Some.FlagConsumer();

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void TryBool_Returns_True_For_Empty_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings()));

        var success = consumer.TryBool("--bool", "info", out var actual);

        Assert.IsTrue(success);
        Assert.IsTrue(actual);
    }

    [TestMethod]
    public void TryBool_Returns_False_For_Invalid_Flag()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings("not-a-bool")));

        var success = consumer.TryBool("--bool", "info", out _);

        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryBool_Returns_Same_Value_When_Asked_Again()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings()));

        Assert.IsTrue(consumer.TryBool("--bool", "info", out var actual1));
        Assert.IsTrue(actual1);

        Assert.IsTrue(consumer.TryBool("--bool", "info", out var actual2));
        Assert.IsTrue(actual2);
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
    public void HelpNeeded_Returns_True_For_Unconsumed_Arguments()
    {
        var consumer = Some.FlagConsumer(
            ("--list", Some.Strings("element")));

        Assert.IsTrue(consumer.HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_True_For_Invalid_Arguments()
    {
        var consumer = Some.FlagConsumer(
            ("--bool", Some.Strings("not-a-bool")));

        Assert.IsFalse(consumer.TryBool("--bool", "info", out _));
        Assert.IsTrue(consumer.HelpNeeded(out _));
    }

    [TestMethod]
    public void HelpNeeded_Returns_True_For_Missing_Arguments()
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
