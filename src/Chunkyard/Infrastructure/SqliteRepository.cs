namespace Chunkyard.Infrastructure;

/// <summary>
/// An implementation of <see cref="IRepository"/> using SQLite.
/// </summary>
public static class SqliteRepository
{
    public static Repository Create(string file)
    {
        var sqliteFile = Path.GetFullPath(file);
        var connectionString = $"Data Source={sqliteFile}";

        if (!File.Exists(sqliteFile))
        {
            DirectoryUtils.EnsureParent(sqliteFile);
            var sqlite = new SqliteUtil(connectionString);

            sqlite.Command(
                @"CREATE TABLE Chunk(ChunkId TEXT PRIMARY KEY, Content BLOB NOT NULL);
                  CREATE TABLE Reference(ReferenceId INTEGER PRIMARY KEY, Content BLOB NOT NULL)");
        }

        return new Repository(
            new SqliteReferenceRepository(connectionString),
            new SqliteChunkRepository(connectionString));
    }
}

internal class SqliteReferenceRepository : IRepository<int>
{
    private readonly SqliteUtil _sqlite;

    public SqliteReferenceRepository(string connectionString)
    {
        _sqlite = new SqliteUtil(connectionString);
    }

    public void StoreValue(int key, byte[] value)
    {
        _sqlite.Command(
            "INSERT INTO Reference (ReferenceId, Content) VALUES($referenceId, $content)",
            new SqliteParameter("$referenceId", key),
            new SqliteParameter("$content", value));
    }

    public void StoreValueIfNotExists(int key, byte[] value)
    {
        _sqlite.Command(
            "INSERT OR IGNORE INTO Reference (ReferenceId, Content) VALUES($referenceId, $content)",
            new SqliteParameter("$referenceId", key),
            new SqliteParameter("$content", value));
    }

    public byte[] RetrieveValue(int key)
    {
        using var stream = _sqlite
            .Query(
                "SELECT Content FROM Reference WHERE ReferenceId = $referenceId",
                reader => reader.GetStream(0),
                new SqliteParameter("$referenceId", key))
            .First();

        return ((MemoryStream)stream).ToArray();
    }

    public bool ValueExists(int key)
    {
        return _sqlite
            .Query(
                "SELECT ReferenceId FROM Reference WHERE ReferenceId = $referenceId",
                reader => reader.GetInt32(0),
                new SqliteParameter("$referenceId", key))
            .Any();
    }

    public IReadOnlyCollection<int> ListKeys()
    {
        return _sqlite
            .Query(
                "SELECT ReferenceId FROM Reference ORDER BY ReferenceId",
                reader => reader.GetInt32(0))
            .ToArray();
    }

    public void RemoveValue(int key)
    {
        _sqlite.Command(
            "DELETE FROM Reference WHERE ReferenceId = $referenceId",
            new SqliteParameter("$referenceId", key));
    }
}

internal class SqliteChunkRepository : IRepository<string>
{
    private readonly SqliteUtil _sqlite;

    public SqliteChunkRepository(string connectionString)
    {
        _sqlite = new SqliteUtil(connectionString);
    }

    public void StoreValue(string key, byte[] value)
    {
        _sqlite.Command(
            "INSERT INTO Chunk (ChunkId, Content) VALUES($chunkId, $content)",
            new SqliteParameter("$chunkId", key),
            new SqliteParameter("$content", value));
    }

    public void StoreValueIfNotExists(string key, byte[] value)
    {
        _sqlite.Command(
            "INSERT OR IGNORE INTO Chunk (ChunkId, Content) VALUES($chunkId, $content)",
            new SqliteParameter("$chunkId", key),
            new SqliteParameter("content", value));
    }

    public byte[] RetrieveValue(string key)
    {
        using var stream = _sqlite
            .Query(
                "SELECT Content FROM Chunk WHERE ChunkId = $chunkId",
                reader => reader.GetStream(0),
                new SqliteParameter("$chunkId", key))
            .First();

        return ((MemoryStream)stream).ToArray();
    }

    public bool ValueExists(string key)
    {
        return _sqlite
            .Query(
                "SELECT ChunkId FROM Chunk WHERE ChunkId = $chunkId",
                reader => reader.GetString(0),
                new SqliteParameter("$chunkId", key))
            .Any();
    }

    public IReadOnlyCollection<string> ListKeys()
    {
        return _sqlite
            .Query(
                "SELECT ChunkId FROM Chunk ORDER BY ChunkId",
                reader => reader.GetString(0))
            .ToArray();
    }

    public void RemoveValue(string key)
    {
        _sqlite.Command(
            "DELETE FROM Chunk WHERE ChunkId = $chunkId",
            new SqliteParameter("$chunkId", key));
    }
}

internal class SqliteUtil
{
    private readonly string _connectionString;

    public SqliteUtil(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Command(
        string query,
        params SqliteParameter[] parameters)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, query, parameters);

        command.ExecuteNonQuery();
    }

    public IEnumerable<T> Query<T>(
        string query,
        Func<SqliteDataReader, T> read,
        params SqliteParameter[] parameters)
    {
        using var connection = OpenConnection();
        using var command = CreateCommand(connection, query, parameters);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            yield return read(reader);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
    private static SqliteCommand CreateCommand(
        SqliteConnection connection,
        string query,
        SqliteParameter[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.Parameters.AddRange(parameters);

        return command;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);

        connection.Open();

        return connection;
    }
}
