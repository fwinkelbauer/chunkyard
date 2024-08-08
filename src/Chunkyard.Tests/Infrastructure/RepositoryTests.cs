namespace Chunkyard.Tests.Infrastructure;

public sealed class MemoryRepositoryTests : RepositoryTests
{
    public MemoryRepositoryTests()
        : base(new MemoryRepository<string>())
    {
    }
}

public sealed class FileRepositoryTests : RepositoryTests
{
    public FileRepositoryTests()
        : base(new FileRepository<string>(Some.Directory(), k => k, f => f))
    {
    }
}

public abstract class RepositoryTests
{
    protected RepositoryTests(IRepository<string> repository)
    {
        Repository = repository;
    }

    protected IRepository<string> Repository { get; }

    [Fact]
    public void Repository_Can_Read_Write()
    {
        Assert.Empty(Repository.UnorderedList());

        var dict = new Dictionary<string, byte[]>
        {
            { "aa", new byte[] { 0x00 } },
            { "bb", new byte[] { 0x01 } },
            { "cc", new byte[] { 0x02 } }
        };

        foreach (var pair in dict)
        {
            Repository.Store(pair.Key, pair.Value);

            Assert.True(Repository.Exists(pair.Key));
            Assert.Equal(pair.Value, Repository.Retrieve(pair.Key));
        }

        Assert.Equal(dict.Keys, Repository.UnorderedList().OrderBy(k => k));

        foreach (var pair in dict)
        {
            Repository.Remove(pair.Key);

            Assert.False(Repository.Exists(pair.Key));
        }

        Assert.Empty(Repository.UnorderedList());
    }

    [Fact]
    public void Repository_Handles_Parallel_Operations()
    {
        var input = new ConcurrentDictionary<string, byte[]>();
        var output = new ConcurrentDictionary<string, byte[]>();

        for (var i = 0; i < 100; i++)
        {
            input.TryAdd($"{i}", RandomNumberGenerator.GetBytes(i));
        }

        Parallel.ForEach(
            input,
            pair => Repository.Store(pair.Key, pair.Value));

        Parallel.ForEach(
            Repository.UnorderedList(),
            key => output.TryAdd(key, Repository.Retrieve(key)));

        Assert.Equal(input, output);
    }
}
