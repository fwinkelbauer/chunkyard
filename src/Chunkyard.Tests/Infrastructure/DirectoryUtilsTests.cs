namespace Chunkyard.Tests.Infrastructure;

public static class DirectoryUtilsTests
{
    public static TheoryData<string[], string> TheoryData => new()
    {
        { new[] { "/foo/bar" }, "/foo" },
        { new[] { "foo/bar" }, "foo" },
        { new[] { "/foo/bar", "/foo/baz" }, "/foo" },
        { new[] { "foo/bar", "foo/baz" }, "foo" },
        { new[] { "foo/bar/hurr", "foo/baz" }, "foo" },
        { new[] { "foo/bar", "foo/bar" }, "foo/bar" },
        { new[] { "foo/bar/hurr", "foo/bar/durr" }, "foo/bar" },
        { new[] { "foo", "bar" }, "" },
        { new[] { "/foo", "/bar" }, "/" },
        { new[] { "C:/foo", "D:/bar" }, "" },
        { new[] { "C:/foo", "C:/bar" }, "C:" }
    };

    [Theory, MemberData(nameof(TheoryData))]
    public static void GetCommonParent_Returns_Parent(
        string[] paths,
        string expectedParent)
    {
        Assert.Equal(
            expectedParent,
            DirectoryUtils.GetCommonParent(paths, '/'));
    }
}
