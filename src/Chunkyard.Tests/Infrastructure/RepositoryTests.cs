namespace Chunkyard.Tests.Infrastructure;

public static class RepositoryTests
{
    [Fact]
    public static void MemoryRepository_Can_Read_Write()
    {
        var repository = new MemoryRepository();

        Repository_Can_Read_Write(repository.Chunks);
        Repository_Can_Read_Write(repository.Log);
    }

    [Fact]
    public static void FileRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();

        var repository = new FileRepository(directory.Name);

        Repository_Can_Read_Write(repository.Chunks);
        Repository_Can_Read_Write(repository.Log);
    }

    [Fact]
    public static void MemoryRepository_StoreValue_Throws_When_Writing_To_Same_Key()
    {
        var repository = new MemoryRepository();

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<ArgumentException>(
            repository.Chunks);

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<ArgumentException>(
            repository.Log);
    }

    [Fact]
    public static void FileRepository_StoreValue_Throws_When_Writing_To_Same_Key()
    {
        using var directory = new DisposableDirectory();

        var repository = new FileRepository(directory.Name);

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<IOException>(
            repository.Chunks);

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<IOException>(
            repository.Log);
    }

    [Fact]
    public static void MemoryRepository_StoreValueIfNotExists_Writes_Key_Once()
    {
        var repository = new MemoryRepository();

        Repository_StoreValueIfNotExists_Writes_Key_Once(repository.Chunks);
        Repository_StoreValueIfNotExists_Writes_Key_Once(repository.Log);
    }

    [Fact]
    public static void FileRepository_StoreValueIfNotExists_Writes_Key_Once()
    {
        using var directory = new DisposableDirectory();

        var repository = new FileRepository(directory.Name);

        Repository_StoreValueIfNotExists_Writes_Key_Once(repository.Chunks);
        Repository_StoreValueIfNotExists_Writes_Key_Once(repository.Log);
    }

    [Fact]
    public static void FileRepository_Prevents_Directory_Traversal_Attack()
    {
        using var directory = new DisposableDirectory();

        var repository = new FileRepository(directory.Name);
        var invalidKey = "../some-file";

        Assert.Throws<ArgumentException>(
            () => repository.Chunks.StoreValue(invalidKey, new byte[] { 0xFF }));

        Assert.Throws<ArgumentException>(
            () => repository.Chunks.RetrieveValue(invalidKey));

        Assert.Throws<ArgumentException>(
            () => repository.Chunks.ValueExists(invalidKey));

        Assert.Throws<ArgumentException>(
            () => repository.Chunks.RemoveValue(invalidKey));
    }

    [Fact]
    public static void MemoryRepository_Sorts_Listed_Keys()
    {
        var repository = new MemoryRepository();

        Repository_Sorts_Listed_Keys(repository.Chunks);
        OrderedRepository_Sorts_Listed_Keys(repository.Log);
    }

    [Fact]
    public static void FileRepository_Sorts_Listed_Keys()
    {
        using var directory = new DisposableDirectory();

        var repository = new FileRepository(directory.Name);

        Repository_Sorts_Listed_Keys(repository.Chunks);
        OrderedRepository_Sorts_Listed_Keys(repository.Log);
    }

    private static void Repository_Sorts_Listed_Keys(IRepository<string> repository)
    {
        Assert.Empty(repository.ListKeys());

        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue("dd", bytes);
        repository.StoreValue("cc", bytes);
        repository.StoreValue("aa", bytes);
        repository.StoreValue("bb", bytes);

        Assert.Equal(
            new[] { "aa", "bb", "cc", "dd" },
            repository.ListKeys());
    }

    private static void OrderedRepository_Sorts_Listed_Keys(
        IOrderedRepository<int> repository)
    {
        Assert.Null(repository.RetrieveLastKey());
        Assert.Empty(repository.ListKeys());

        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(4, bytes);
        repository.StoreValue(3, bytes);
        repository.StoreValue(1, bytes);
        repository.StoreValue(2, bytes);

        Assert.Equal(
            new[] { 1, 2, 3, 4 },
            repository.ListKeys());

        Assert.Equal(4, repository.RetrieveLastKey());
    }

    private static void Repository_StoreValue_Throws_When_Writing_To_Same_Key<TException>(
        IRepository<string> repository)
        where TException : Exception
    {
        var key = "aa";
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(key, bytes);

        Assert.Throws<TException>(
            () => repository.StoreValue(key, bytes));
    }

    private static void Repository_StoreValue_Throws_When_Writing_To_Same_Key<TException>(
        IRepository<int> repository)
        where TException : Exception
    {
        var key = 15;
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(key, bytes);

        Assert.Throws<TException>(
            () => repository.StoreValue(key, bytes));
    }

    private static void Repository_StoreValueIfNotExists_Writes_Key_Once(
        IRepository<string> repository)
    {
        var key = "aa";
        var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValueIfNotExists(key, expectedBytes);

        repository.StoreValueIfNotExists(
            key,
            new byte[] { 0xAA, 0xAA, 0xAA, 0xAA });

        Assert.Equal(expectedBytes, repository.RetrieveValue(key));
    }

    private static void Repository_StoreValueIfNotExists_Writes_Key_Once(
        IRepository<int> repository)
    {
        var key = 15;
        var expectedBytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValueIfNotExists(key, expectedBytes);

        repository.StoreValueIfNotExists(
            key,
            new byte[] { 0xAA, 0xAA, 0xAA, 0xAA });

        Assert.Equal(expectedBytes, repository.RetrieveValue(key));
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

        Assert.Equal(new[] { key1, key2 }, repository.ListKeys());

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

        Assert.Equal(new[] { 1, 2 }, repository.ListKeys());

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
