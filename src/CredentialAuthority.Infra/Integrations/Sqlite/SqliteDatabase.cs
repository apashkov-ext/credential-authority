using Microsoft.Data.Sqlite;

namespace CredentialAuthority.Infra.Integrations.Sqlite;

public sealed class SqliteDatabase
{
    private readonly string _path;
    private readonly string _connectionString;

    public SqliteDatabase(string path)
    {
        _path = System.IO.Path.GetFullPath(path);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _path,
            ForeignKeys = true,
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
    }

    public async Task<SqliteConnection> OpenConnection(CancellationToken ct = default)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(ct);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task Initialize(CancellationToken ct = default)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        await using var connection = await OpenConnection(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;

            CREATE TABLE IF NOT EXISTS requests (
                id TEXT NOT NULL PRIMARY KEY,
                owner_name TEXT NOT NULL,
                owner_key TEXT NOT NULL,
                issuer TEXT NOT NULL,
                valid_until TEXT NOT NULL,
                status TEXT NOT NULL CHECK (status IN ('Pending', 'Issued', 'Rejected')),
                serial_number TEXT UNIQUE,
                issued_at TEXT,
                rejection_reason TEXT,
                CHECK (
                    (status = 'Pending' AND serial_number IS NULL AND issued_at IS NULL AND rejection_reason IS NULL)
                    OR (status = 'Issued' AND serial_number IS NOT NULL AND issued_at IS NOT NULL AND rejection_reason IS NULL)
                    OR (status = 'Rejected' AND serial_number IS NULL AND issued_at IS NULL
                        AND rejection_reason IS NOT NULL AND rejection_reason = 'ValidityExpired')
                )
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_requests_reserved_owner
                ON requests(owner_key) WHERE status IN ('Pending', 'Issued');
            CREATE INDEX IF NOT EXISTS ix_requests_pending
                ON requests(id) WHERE status = 'Pending';
            """;
        await command.ExecuteNonQueryAsync(ct);
    }
}
