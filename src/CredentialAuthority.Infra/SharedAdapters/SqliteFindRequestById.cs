using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.SharedPorts;
using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Infra.SharedAdapters;

internal sealed class SqliteFindRequestById : IFindRequestById
{
    private readonly SqliteDatabase _database;

    public SqliteFindRequestById(SqliteDatabase database)
    {
        _database = database;
    }

    public async Task<IssuanceRequest?> Execute(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _database.OpenConnection(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM requests WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? SqliteRequestReader.Read(reader) : null;
    }
}
