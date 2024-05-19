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
    private static DisposableDirectory? _tempDirectory;

    public FileRepositoryTests()
        : base(CreateRepository())
    {
    }

    public void Dispose()
    {
        _tempDirectory?.Dispose();
        _tempDirectory = null;
    }

    private static IRepository<string> CreateRepository()
    {
        _tempDirectory = new();

        return new FileRepository<string>(
            _tempDirectory.Name,
            key => key,
            file => file);
    }
}

public abstract class RepositoryTests
{
    private readonly IReadOnlyCollection<string> _keys;

    protected RepositoryTests(IRepository<string> repository)
    {
        Repository = repository;

        _keys = new[] { "aa", "bb", "cc" };
    }

    protected IRepository<string> Repository { get; }

    [Fact]
    public void Repository_Can_Read_Write()
    {
        Assert.Empty(Repository.UnorderedList());
        Assert.False(Repository.TryLast(out _));

        var dict = _keys.ToDictionary(
            k => k,
            k => SHA256.HashData(Encoding.UTF8.GetBytes(k)));

        foreach (var pair in dict)
        {
            Repository.Store(pair.Key, pair.Value);

            Assert.True(Repository.Exists(pair.Key));
            Assert.Equal(pair.Value, Repository.Retrieve(pair.Key));
        }

        Assert.Equal(_keys, Repository.UnorderedList().OrderBy(k => k));
        Assert.True(Repository.TryLast(out var maxKey));
        Assert.Equal(Repository.UnorderedList().Max(), maxKey);

        foreach (var pair in dict)
        {
            Repository.Remove(pair.Key);

            Assert.False(Repository.Exists(pair.Key));
        }

        Assert.Empty(Repository.UnorderedList());
    }

    [Fact]
    public void Repository_Store_Throws_When_Writing_To_Same_Key()
    {
        var key = _keys.First();
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
            input.TryAdd($"{i}", RandomNumberGenerator.GetBytes(i));
        }

        Parallel.ForEach(
            input,
            pair => Repository.Store(pair.Key, pair.Value));

        var output = new ConcurrentDictionary<string, byte[]>();

        Parallel.ForEach(
            Repository.UnorderedList(),
            key => output.TryAdd(key, Repository.Retrieve(key)));

        Assert.Equal(input, output);
    }
}
