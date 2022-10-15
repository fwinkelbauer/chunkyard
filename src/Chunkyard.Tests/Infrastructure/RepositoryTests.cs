namespace Chunkyard.Tests.Infrastructure;

public static class RepositoryTests
{
    [Fact]
    public static void MemoryRepository_Can_Read_Write()
    {
        Repository_Can_Read_Write(new MemoryRepository<string>());
        Repository_Can_Read_Write(new MemoryRepository<int>());
    }

    [Fact]
    public static void FileRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();

        Repository_Can_Read_Write(
            FileRepository.CreateChunkRepository(directory.Name));

        Repository_Can_Read_Write(
            FileRepository.CreateReferenceRepository(directory.Name));
    }

    [Fact]
    public static void MemoryRepository_StoreValue_Throws_When_Writing_To_Same_Key()
    {
        Repository_StoreValue_Throws_When_Writing_To_Same_Key<ArgumentException>(
            new MemoryRepository<string>());

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<ArgumentException>(
            new MemoryRepository<int>());
    }

    [Fact]
    public static void FileRepository_StoreValue_Throws_When_Writing_To_Same_Key()
    {
        using var directory = new DisposableDirectory();

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<IOException>(
            FileRepository.CreateChunkRepository(directory.Name));

        Repository_StoreValue_Throws_When_Writing_To_Same_Key<IOException>(
            FileRepository.CreateReferenceRepository(directory.Name));
    }

    [Fact]
    public static void MemoryRepository_StoreValueIfNotExists_Writes_Key_Once()
    {
        Repository_StoreValueIfNotExists_Writes_Key_Once(
            new MemoryRepository<string>());

        Repository_StoreValueIfNotExists_Writes_Key_Once(
            new MemoryRepository<int>());
    }

    [Fact]
    public static void FileRepository_StoreValueIfNotExists_Writes_Key_Once()
    {
        using var directory = new DisposableDirectory();

        Repository_StoreValueIfNotExists_Writes_Key_Once(
            FileRepository.CreateChunkRepository(directory.Name));

        Repository_StoreValueIfNotExists_Writes_Key_Once(
            FileRepository.CreateReferenceRepository(directory.Name));
    }

    [Fact]
    public static void FileRepository_Prevents_Directory_Traversal_Attack()
    {
        using var directory = new DisposableDirectory();

        var repository = FileRepository.CreateChunkRepository(directory.Name);
        var invalidKey = "../some-file";

        Assert.Throws<ArgumentException>(
            () => repository.StoreValue(invalidKey, new byte[] { 0xFF }));

        Assert.Throws<ArgumentException>(
            () => repository.RetrieveValue(invalidKey));

        Assert.Throws<ArgumentException>(
            () => repository.ValueExists(invalidKey));

        Assert.Throws<ArgumentException>(
            () => repository.RemoveValue(invalidKey));
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

        Assert.Equal(new[] { 1, 2 }, repository.ListKeys().OrderBy(k => k));

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
