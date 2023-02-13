namespace Chunkyard.Tests.Infrastructure;

public sealed class MemoryRepositoryTests
    : RepositoryTests
{
    public MemoryRepositoryTests()
        : base(new MemoryRepository<string>())
    {
    }
}

public sealed class FileRepositoryTests
    : RepositoryTests, IDisposable
{
    private static DisposableDirectory? TempDirectory;

    public FileRepositoryTests()
        : base(CreateRepository())
    {
    }

    [Fact]
    public void Methods_Prevent_Directory_Traversal_Attack()
    {
        var invalidKey = "../some-file";

        Assert.Throws<ArgumentException>(
            () => Repository.Store(invalidKey, new byte[] { 0xFF }));

        Assert.Throws<ArgumentException>(
            () => Repository.Retrieve(invalidKey));

        Assert.Throws<ArgumentException>(
            () => Repository.Exists(invalidKey));

        Assert.Throws<ArgumentException>(
            () => Repository.Remove(invalidKey));
    }

    public void Dispose()
    {
        TempDirectory?.Dispose();
        TempDirectory = null;
    }

    private static IRepository<string> CreateRepository()
    {
        TempDirectory = new DisposableDirectory();

        return new FileRepository<string>(
            TempDirectory.Name,
            key => key,
            file => file);
    }
}

public abstract class RepositoryTests
{
    internal RepositoryTests(IRepository<string> repository)
    {
        Repository = repository;
        Keys = new[] { "aa", "bb", "cc" };
    }

    internal IRepository<string> Repository { get; }

    internal IReadOnlyCollection<string> Keys { get; }

    [Fact]
    public void Repository_Can_Read_Write()
    {
        Assert.Empty(Repository.List());
        Assert.False(Repository.TryLast(out _));

        var dict = Keys.ToDictionary(
            k => k,
            k => SHA256.HashData(Encoding.UTF8.GetBytes(k)));

        foreach (var pair in dict)
        {
            Repository.Store(pair.Key, pair.Value);

            Assert.True(Repository.Exists(pair.Key));
            Assert.Equal(pair.Value, Repository.Retrieve(pair.Key));
        }

        Assert.Equal(Keys, Repository.List().OrderBy(k => k));
        Assert.True(Repository.TryLast(out var maxKey));
        Assert.Equal(Repository.List().Max(), maxKey);

        foreach (var pair in dict)
        {
            Repository.Remove(pair.Key);

            Assert.False(Repository.Exists(pair.Key));
        }

        Assert.Empty(Repository.List());
    }

    [Fact]
    public void Repository_StoreIfNotExists_Writes_Key_Once()
    {
        var key = Keys.First();
        var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Repository.StoreIfNotExists(key, expectedBytes);

        Repository.StoreIfNotExists(
            key,
            new byte[] { 0xAA, 0xAA, 0xAA, 0xAA });

        Assert.Equal(expectedBytes, Repository.Retrieve(key));
    }

    [Fact]
    public void Repository_Store_Throws_When_Writing_To_Same_Key()
    {
        var key = Keys.First();
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Repository.Store(key, bytes);

        Assert.ThrowsAny<Exception>(
            () => Repository.Store(key, bytes));
    }

    [Fact]
    public void Repository_Handles_Parallel_Operations()
    {
        var input = new ConcurrentDictionary<string, byte[]>();

        for (var i = 0; i < 100; i++)
        {
            input.TryAdd($"{i}", Some.RandomNumber(i));
        }

        Parallel.ForEach(
            input,
            pair => Repository.Store(pair.Key, pair.Value));

        var output = new ConcurrentDictionary<string, byte[]>();

        Parallel.ForEach(
            Repository.List(),
            key => output.TryAdd(key, Repository.Retrieve(key)));

        Assert.Equal(input, output);
    }
}
