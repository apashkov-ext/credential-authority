using CredentialAuthority.App.Features.IssueCredential;
using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Infra.Features.IssueCredential;

internal sealed class SqliteGetPendingRequestIds : IGetPendingRequestIds
{
    private readonly SqliteDatabase _database;

    public SqliteGetPendingRequestIds(SqliteDatabase database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<Guid>> Execute(CancellationToken ct = default)
    {
        await using var connection = await _database.OpenConnection(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM requests WHERE status = 'Pending' ORDER BY rowid;";
        await using var reader = await command.ExecuteReaderAsync(ct);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(ct))
        {
            ids.Add(Guid.Parse(reader.GetString(0)));
        }

        return ids;
    }
}
