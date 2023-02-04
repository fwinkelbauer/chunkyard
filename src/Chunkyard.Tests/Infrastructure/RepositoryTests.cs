namespace Chunkyard.Tests.Infrastructure;

public sealed class StringMemoryRepositoryTests
    : StringRepositoryTests
{
    public StringMemoryRepositoryTests()
        : base(new MemoryRepository<string>())
    {
    }
}

public sealed class IntMemoryRepositoryTests
    : IntRepositoryTests
{
    public IntMemoryRepositoryTests()
        : base(new MemoryRepository<int>())
    {
    }
}

public sealed class StringFileRepositoryTests
    : StringRepositoryTests, IDisposable
{
    private static DisposableDirectory? TempDirectory;

    public StringFileRepositoryTests()
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

public sealed class IntFileRepositoryTests
    : IntRepositoryTests, IDisposable
{
    private static DisposableDirectory? TempDirectory;

    public IntFileRepositoryTests()
        : base(CreateRepository())
    {
    }

    public void Dispose()
    {
        TempDirectory?.Dispose();
        TempDirectory = null;
    }

    private static IRepository<int> CreateRepository()
    {
        TempDirectory = new DisposableDirectory();

        return new FileRepository<int>(
            TempDirectory.Name,
            number => number.ToString(),
            Convert.ToInt32);
    }
}

public abstract class IntRepositoryTests : RepositoryTests<int>
{
    internal IntRepositoryTests(IRepository<int> repository)
        : base(repository, new[] { 1, 2, 3 })
    {
    }
}

public abstract class StringRepositoryTests : RepositoryTests<string>
{
    internal StringRepositoryTests(IRepository<string> repository)
        : base(repository, new[] { "aa", "bb", "cc" })
    {
    }
}

public abstract class RepositoryTests<T>
    where T : notnull
{
    internal RepositoryTests(
        IRepository<T> repository,
        IReadOnlyCollection<T> keys)
    {
        Repository = repository;
        Keys = keys;
    }

    internal IRepository<T> Repository { get; }

    internal IReadOnlyCollection<T> Keys { get; }

    [Fact]
    public void Repository_Can_Read_Write()
    {
        Assert.NotEmpty(Keys);
        Assert.Empty(Repository.ListKeys());

        var dict = Keys.ToDictionary(
            k => k,
            k => SHA256.HashData(Encoding.UTF8.GetBytes(k.ToString()!)));

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
}
