using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using DbUp;
using Microsoft.Data.Sqlite;

namespace Chunkyard
{
    /// <summary>
    /// An implementation of <see cref="IRepository"/> using SQLite.
    /// </summary>
    internal class SqliteRepository : IRepository
    {
        private readonly string _database;
        private readonly string _connectionString;

        private bool _dbUpRanOnce;

        public SqliteRepository(string database)
        {
            _database = database;
            _connectionString = $"Data Source={_database}";

            _dbUpRanOnce = false;

            RepositoryUri = new Uri($"sqlite://database");
            RepositoryId = LoadRepositoryId();
        }

        public Uri RepositoryUri { get; }

        public Guid RepositoryId { get; }

        public bool StoreValue(Uri contentUri, byte[] value)
        {
            using var connection = OpenConnection();

            var rows = connection.Execute(
                @"INSERT OR IGNORE INTO Content(ContentUri, Value)
                  VALUES(@ContentUri, @Value)",
                new { ContentUri = contentUri.ToString(), Value = value });

            return rows == 1;
        }

        public byte[] RetrieveValue(Uri contentUri)
        {
            using var connection = OpenConnection();

            return connection.QuerySingle<byte[]>(
                @"SELECT Value
                  FROM Content
                  WHERE ContentUri = @ContentUri",
                new { ContentUri = contentUri.ToString() });
        }

        public bool ValueExists(Uri contentUri)
        {
            using var connection = OpenConnection();

            var count = connection.QuerySingle<int>(
                @"SELECT Count(ContentUri)
                  FROM Content
                  WHERE ContentUri = @ContentUri",
                new { ContentUri = contentUri.ToString() });

            return count == 1;
        }

        public Uri[] ListUris()
        {
            using var connection = OpenConnection();

            return connection.Query<string>(
                @"SELECT ContentUri
                  FROM Content")
                .Select(text => new Uri(text))
                .ToArray();
        }

        public void RemoveValue(Uri contentUri)
        {
            using var connection = OpenConnection();

            connection.Execute(
                @"DELETE FROM Content
                  WHERE ContentUri = @ContentUri",
                new { ContentUri = contentUri.ToString() });
        }

        public int AppendToLog(int newLogPosition, byte[] value)
        {
            using var connection = OpenConnection();

            connection.Execute(
                @"INSERT INTO Reflog(LogId, Value)
                  VALUES(@LogId, @Value)",
                new { LogId = newLogPosition, Value = value });

            return newLogPosition;
        }

        public byte[] RetrieveFromLog(int logPosition)
        {
            using var connection = OpenConnection();

            return connection.QuerySingle<byte[]>(
                @"SELECT Value
                  FROM Reflog
                  WHERE LogId = @LogId",
                new { LogId = logPosition });
        }

        public void RemoveFromLog(int logPosition)
        {
            using var connection = OpenConnection();

            connection.Execute(
                @"DELETE FROM Reflog
                  WHERE LogId = @LogId",
                new { LogId = logPosition });
        }

        public int[] ListLogPositions()
        {
            using var connection = OpenConnection();

            return connection.Query<int>(
                @"SELECT LogId
                  FROM Reflog")
                .ToArray();
        }

        private Guid LoadRepositoryId()
        {
            using var connection = OpenConnection();
            var repositoryId = 1;

            connection.Execute(
                @"INSERT OR IGNORE INTO Repository(RepositoryId, Guid)
                  VALUES(@RepositoryId, @Guid)",
                new { RepositoryId = repositoryId, Guid = Guid.NewGuid().ToString() });

            var guidText = connection.QuerySingle<string>(
                @"SELECT Guid
                  FROM Repository
                  WHERE RepositoryId = @RepositoryId",
                new { RepositoryId = repositoryId });

            return Guid.Parse(guidText);
        }

        private SqliteConnection OpenConnection()
        {
            if (!_dbUpRanOnce)
            {
                var parentDir = Directory.GetParent(_database);

                if (parentDir != null && !parentDir.Exists)
                {
                    parentDir.Create();
                }

                var upgrader = DeployChanges.To
                    .SQLiteDatabase(_connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToNowhere()
                    .Build();

                var result = upgrader.PerformUpgrade();

                if (!result.Successful)
                {
                    throw new ChunkyardException(
                        "Error while migrating database",
                        result.Error);
                }

                _dbUpRanOnce = true;
            }

            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            return connection;
        }
    }
}
