namespace Chunkyard.Tests.Infrastructure;

public static class DirectoryUtilsTests
{
    [Theory]
    [InlineData(new[] { "/foo/bar" }, "/foo")]
    [InlineData(new[] { "foo/bar" }, "foo")]
    [InlineData(new[] { "/foo/bar", "/foo/baz" }, "/foo")]
    [InlineData(new[] { "foo/bar", "foo/baz" }, "foo")]
    [InlineData(new[] { "foo/bar/hurr", "foo/baz" }, "foo")]
    [InlineData(new[] { "foo/bar", "foo/bar" }, "foo/bar")]
    [InlineData(new[] { "foo/bar/hurr", "foo/bar/durr" }, "foo/bar")]
    [InlineData(new[] { "foo", "bar" }, "")]
    [InlineData(new[] { "/foo", "/bar" }, "/")]
    [InlineData(new[] { "C:/foo", "D:/bar" }, "")]
    [InlineData(new[] { "C:/foo", "C:/bar" }, "C:")]
    public static void GetCommonParent_Returns_Parent(
        string[] paths,
        string expectedParent)
    {
        Assert.Equal(
            expectedParent,
            DirectoryUtils.GetCommonParent(paths, '/'));
    }
}
