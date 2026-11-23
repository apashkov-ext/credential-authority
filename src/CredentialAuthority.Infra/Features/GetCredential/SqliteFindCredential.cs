using System.Globalization;
using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.GetCredential;
using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Infra.Features.GetCredential;

internal sealed class SqliteFindCredential : IFindCredential
{
    private readonly SqliteDatabase _database;

    public SqliteFindCredential(SqliteDatabase database)
    {
        _database = database;
    }

    public async Task<Credential?> Execute(Guid serialNumber, CancellationToken ct = default)
    {
        await using var connection = await _database.OpenConnection(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM requests WHERE serial_number = $serial AND status = 'Issued';";
        command.Parameters.AddWithValue("$serial", serialNumber.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new Credential(serialNumber,
            OwnerName.Create(reader.GetString(reader.GetOrdinal("owner_name"))),
            reader.GetString(reader.GetOrdinal("issuer")),
            DateTimeOffset.ParseExact(reader.GetString(reader.GetOrdinal("issued_at")), "O", CultureInfo.InvariantCulture),
            DateTimeOffset.ParseExact(reader.GetString(reader.GetOrdinal("valid_until")), "O", CultureInfo.InvariantCulture));
    }
}
