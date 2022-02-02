namespace Chunkyard.Tests.Infrastructure;

public static class RepositoryTests
{
    [Fact]
    public static void MemoryIntRepository_Can_Read_Write()
    {
        IntRepository_Can_Read_Write(new MemoryRepository<int>());
    }

    [Fact]
    public static void MemoryUriRepository_Can_Read_Write()
    {
        UriRepository_Can_Read_Write(new MemoryRepository<Uri>());
    }

    [Fact]
    public static void FileIntRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();
        var repository = FileRepository.CreateIntRepository(
            directory.Name);

        IntRepository_Can_Read_Write(repository);
    }

    [Fact]
    public static void FileUriRepository_Can_Read_Write()
    {
        using var directory = new DisposableDirectory();
        var repository = FileRepository.CreateUriRepository(
            directory.Name);

        UriRepository_Can_Read_Write(repository);
    }

    [Fact]
    public static void FileUriRepository_Prevents_Directory_Traversal_Attack()
    {
        using var directory = new DisposableDirectory();
        var repository = FileRepository.CreateUriRepository(
            directory.Name);

        var invalidUri = new Uri("sha256://../some-file");

        Assert.Throws<ChunkyardException>(
            () => repository.StoreValue(invalidUri, new byte[] { 0xFF }));

        Assert.Throws<ChunkyardException>(
            () => repository.RetrieveValue(invalidUri));

        Assert.Throws<ChunkyardException>(
            () => repository.ValueExists(invalidUri));

        Assert.Throws<ChunkyardException>(
            () => repository.RemoveValue(invalidUri));
    }

    [Fact]
    public static void MemoryRepository_Throws_When_Writing_To_Same_Key()
    {
        var repository = new MemoryRepository<Uri>();

        var uri = new Uri("sha256://aa");
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(uri, bytes);

        Assert.Throws<ArgumentException>(
            () => repository.StoreValue(uri, bytes));
    }

    [Fact]
    public static void FileRepository_Throws_When_Writing_To_Same_Key()
    {
        using var directory = new DisposableDirectory();
        var repository = FileRepository.CreateUriRepository(
            directory.Name);

        var uri = new Uri("sha256://aa");
        var bytes = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        repository.StoreValue(uri, bytes);

        Assert.Throws<IOException>(
            () => repository.StoreValue(uri, bytes));
    }

    private static void UriRepository_Can_Read_Write(
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

    private static void IntRepository_Can_Read_Write(
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
