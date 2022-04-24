namespace Chunkyard.Tests.Infrastructure;

public static class RepositoryTests
{
    [Fact]
    public static void MemoryRepository_Can_Read_Write()
    {
        var repository = new MemoryRepository();

        Repository_Can_Read_Write(repository.Chunks);
        Repository_Can_Read_Write(repository.Snapshots);
    }

    [Fact]
    public static void FileRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();
        var repository = new FileRepository(directory.Name);

        Repository_Can_Read_Write(repository.Chunks);
        Repository_Can_Read_Write(repository.Snapshots);
    }

    [Fact]
    public static void MemoryRepository_Throws_When_Writing_To_Same_Key()
    {
        var repository = new MemoryRepository();

        Repository_Throws_When_Writing_To_Same_Key<ArgumentException>(
            repository.Chunks);
    }

    [Fact]
    public static void FileRepository_Throws_When_Writing_To_Same_Key()
    {
        using var directory = new DisposableDirectory();
        var repository = new FileRepository(directory.Name);

        Repository_Throws_When_Writing_To_Same_Key<IOException>(
            repository.Chunks);
    }

    [Fact]
    public static void FileRepository_Prevents_Directory_Traversal_Attack()
    {
        using var directory = new DisposableDirectory();
        var repository = new FileRepository(directory.Name);

        var invalidKey = "../some-file";

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.StoreValue(invalidKey, new byte[] { 0xFF }));

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.RetrieveValue(invalidKey));

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.ValueExists(invalidKey));

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.RemoveValue(invalidKey));
    }

    private static void Repository_Throws_When_Writing_To_Same_Key<TException>(
        IRepository<string> repository)
        where TException : Exception
    {
        var key = "aa";
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(key, bytes);

        Assert.Throws<TException>(
            () => repository.StoreValue(key, bytes));
    }

    private static void Repository_Can_Read_Write(
        IRepository<string> repository)
    {
        var expectedBytes1 = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
        var expectedBytes2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var key1 = "aa";
        var key2 = "bb";

        Assert.Empty(repository.ListKeys());

        repository.StoreValue(key1, expectedBytes1);
        repository.StoreValue(key2, expectedBytes2);

        Assert.Equal(
            new[] { key1, key2 },
            repository.ListKeys().OrderBy(key => key));

        Assert.True(repository.ValueExists(key1));
        Assert.True(repository.ValueExists(key2));

        Assert.Equal(expectedBytes1, repository.RetrieveValue(key1));
        Assert.Equal(expectedBytes2, repository.RetrieveValue(key2));

        repository.RemoveValue(key1);
        repository.RemoveValue(key2);

        Assert.Empty(repository.ListKeys());
        Assert.False(repository.ValueExists(key1));
        Assert.False(repository.ValueExists(key2));
    }

    private static void Repository_Can_Read_Write(
        IRepository<int> repository)
    {
        var expectedBytes1 = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
        var expectedBytes2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Empty(repository.ListKeys());

        repository.StoreValue(1, expectedBytes1);
        repository.StoreValue(2, expectedBytes2);

        Assert.Equal(
            new[] { 1, 2 },
            repository.ListKeys().OrderBy(key => key));

        Assert.True(repository.ValueExists(1));
        Assert.True(repository.ValueExists(2));

        Assert.Equal(expectedBytes1, repository.RetrieveValue(1));
        Assert.Equal(expectedBytes2, repository.RetrieveValue(2));

        repository.RemoveValue(1);
        repository.RemoveValue(2);

        Assert.Empty(repository.ListKeys());
        Assert.False(repository.ValueExists(1));
        Assert.False(repository.ValueExists(2));
    }
}
