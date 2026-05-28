using Microsoft.Data.Sqlite;

namespace SerilogBestPractices.Data;

public static class DbInitializer
{
    public static void Initialize(SqliteConnection connection)
    {
        connection.Open();

        using var walCommand = connection.CreateCommand();
        walCommand.CommandText = "PRAGMA journal_mode=WAL";
        walCommand.ExecuteNonQuery();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Orders (
                Id TEXT PRIMARY KEY,
                CustomerName TEXT NOT NULL,
                Amount REAL NOT NULL,
                CreatedAt TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Created'
            )
            """;
        command.ExecuteNonQuery();
    }
}
