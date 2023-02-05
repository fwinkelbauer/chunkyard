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
            () => Repository.StoreValue(invalidKey, new byte[] { 0xFF }));

        Assert.Throws<ArgumentException>(
            () => Repository.RetrieveValue(invalidKey));

        Assert.Throws<ArgumentException>(
            () => Repository.ValueExists(invalidKey));

        Assert.Throws<ArgumentException>(
            () => Repository.RemoveValue(invalidKey));
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
        Assert.Empty(Repository.ListKeys());

        var dict = Keys.ToDictionary(
            k => k,
            k => SHA256.HashData(Encoding.UTF8.GetBytes(k)));

        foreach (var pair in dict)
        {
            Repository.StoreValue(pair.Key, pair.Value);

            Assert.True(Repository.ValueExists(pair.Key));
            Assert.Equal(pair.Value, Repository.RetrieveValue(pair.Key));
        }

        Assert.Equal(Keys, Repository.ListKeys().OrderBy(k => k));

        foreach (var pair in dict)
        {
            Repository.RemoveValue(pair.Key);

            Assert.False(Repository.ValueExists(pair.Key));
        }

        Assert.Empty(Repository.ListKeys());
    }

    [Fact]
    public void Repository_StoreValueIfNotExists_Writes_Key_Once()
    {
        var key = Keys.First();
        var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Repository.StoreValueIfNotExists(key, expectedBytes);

        Repository.StoreValueIfNotExists(
            key,
            new byte[] { 0xAA, 0xAA, 0xAA, 0xAA });

        Assert.Equal(expectedBytes, Repository.RetrieveValue(key));
    }

    [Fact]
    public void Repository_StoreValue_Throws_When_Writing_To_Same_Key()
    {
        var key = Keys.First();
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Repository.StoreValue(key, bytes);

        Assert.ThrowsAny<Exception>(
            () => Repository.StoreValue(key, bytes));
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
            pair => Repository.StoreValue(pair.Key, pair.Value));

        var output = new ConcurrentDictionary<string, byte[]>();

        Parallel.ForEach(
            Repository.ListKeys(),
            key => output.TryAdd(key, Repository.RetrieveValue(key)));

        Assert.Equal(input, output);
    }
}
