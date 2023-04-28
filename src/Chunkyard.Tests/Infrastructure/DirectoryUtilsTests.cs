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

    [Theory]
    [InlineData("/some", "dir/structure")]
    [InlineData("/some/dir", "structure")]
    [InlineData("/some/dir/structure", "")]
    [InlineData("/other/../some/dir", "structure")]
    public static void CombinePathSafe_Returns_Path(
        string directory,
        string relativePath)
    {
        Assert.Equal(
            Path.Combine(directory, relativePath),
            DirectoryUtils.CombinePathSafe(directory, relativePath));
    }

    [Theory]
    [InlineData("/some/dir", "../invalid")]
    [InlineData("C:/some/dir", "../invalid")]
    [InlineData("some/dir", "../invalid")]
    public static void CombinePathSafe_Throws_On_Directory_Traversal_Of_RelativePath(
        string directory,
        string relativePath)
    {
        Assert.Throws<ArgumentException>(
            () => DirectoryUtils.CombinePathSafe(
                directory,
                relativePath));
    }
}
