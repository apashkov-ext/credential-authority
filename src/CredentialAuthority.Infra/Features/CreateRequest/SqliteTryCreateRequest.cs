using System.Globalization;
using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.CreateRequest;
using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Infra.Features.CreateRequest;

internal sealed class SqliteTryCreateRequest : ITryCreateRequest
{
    private readonly SqliteDatabase _database;

    public SqliteTryCreateRequest(SqliteDatabase database)
    {
        _database = database;
    }

    public async Task<bool> Execute(IssuanceRequest request, CancellationToken ct = default)
    {
        await using var connection = await _database.OpenConnection(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO requests (id, owner_name, owner_key, issuer, valid_until, status)
            VALUES ($id, $name, $key, $issuer, $until, 'Pending')
            ON CONFLICT(owner_key) WHERE status IN ('Pending', 'Issued') DO NOTHING;
            """;
        command.Parameters.AddWithValue("$id", request.Id.ToString("D"));
        command.Parameters.AddWithValue("$name", request.OwnerName.Value);
        command.Parameters.AddWithValue("$key", request.OwnerName.Key);
        command.Parameters.AddWithValue("$issuer", request.Issuer);
        command.Parameters.AddWithValue("$until", request.ValidUntil.ToString("O", CultureInfo.InvariantCulture));
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }
}
