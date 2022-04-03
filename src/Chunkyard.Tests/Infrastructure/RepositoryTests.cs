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

        var invalidUri = new Uri("sha256://../some-file");

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.StoreValue(invalidUri, new byte[] { 0xFF }));

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.RetrieveValue(invalidUri));

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.ValueExists(invalidUri));

        Assert.Throws<ChunkyardException>(
            () => repository.Chunks.RemoveValue(invalidUri));
    }

    private static void Repository_Throws_When_Writing_To_Same_Key<TException>(
        IRepository<Uri> repository)
        where TException : Exception
    {
        var uri = new Uri("sha256://aa");
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(uri, bytes);

        Assert.Throws<TException>(
            () => repository.StoreValue(uri, bytes));
    }

    private static void Repository_Can_Read_Write(
        IRepository<Uri> repository)
    {
        var expectedBytes1 = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
        var expectedBytes2 = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        var uri1 = new Uri("sha256://aa");
        var uri2 = new Uri("sha256://bb");

        Assert.Empty(repository.ListKeys());

        repository.StoreValue(uri1, expectedBytes1);
        repository.StoreValue(uri2, expectedBytes2);

        Assert.Equal(
            new[] { uri1, uri2 },
            repository.ListKeys().OrderBy(u => u.AbsoluteUri));

        Assert.True(repository.ValueExists(uri1));
        Assert.True(repository.ValueExists(uri2));

        Assert.Equal(expectedBytes1, repository.RetrieveValue(uri1));
        Assert.Equal(expectedBytes2, repository.RetrieveValue(uri2));

        repository.RemoveValue(uri1);
        repository.RemoveValue(uri2);

        Assert.Empty(repository.ListKeys());
        Assert.False(repository.ValueExists(uri1));
        Assert.False(repository.ValueExists(uri2));
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
            repository.ListKeys().OrderBy(i => i));

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
