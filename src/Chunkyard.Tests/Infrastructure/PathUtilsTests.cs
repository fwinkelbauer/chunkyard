namespace Chunkyard.Tests.Infrastructure;

[TestClass]
public sealed class PathUtilsTests
{
    [TestMethod]
    [DataRow(new[] { "/foo/bar" }, "/foo")]
    [DataRow(new[] { "foo/bar" }, "foo")]
    [DataRow(new[] { "/foo/bar", "/foo/baz" }, "/foo")]
    [DataRow(new[] { "foo/bar", "foo/baz" }, "foo")]
    [DataRow(new[] { "foo/bar/hurr", "foo/baz" }, "foo")]
    [DataRow(new[] { "foo/bar", "foo/bar" }, "foo/bar")]
    [DataRow(new[] { "foo/bar/hurr", "foo/bar/durr" }, "foo/bar")]
    [DataRow(new[] { "foo", "bar" }, "")]
    [DataRow(new[] { "/foo", "/bar" }, "/")]
    [DataRow(new[] { "C:/foo", "D:/bar" }, "")]
    [DataRow(new[] { "C:/foo", "C:/bar" }, "C:")]
    public void GetCommonParent_Returns_Parent(
        string[] paths,
        string expectedParent)
    {
        Assert.AreEqual(
            expectedParent,
            PathUtils.GetCommonParent(paths, '/'));
    }
}
