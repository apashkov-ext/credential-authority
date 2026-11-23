using System.Globalization;
using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.IssueCredential;
using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Infra.Features.IssueCredential;

internal sealed class SqliteCompleteRequest : ICompleteRequest
{
    private readonly SqliteDatabase _database;

    public SqliteCompleteRequest(SqliteDatabase database)
    {
        _database = database;
    }

    public async Task<bool> Execute(IssuanceRequest request, Credential? credential, CancellationToken ct = default)
    {
        await using var connection = await _database.OpenConnection(ct);
        await using var command = connection.CreateCommand();
        // Keeping the document in the same row makes completion one atomic SQLite statement.
        command.CommandText = """
            UPDATE requests
            SET status = $status, serial_number = $serial, issued_at = $issued, rejection_reason = $reason
            WHERE id = $id AND status = 'Pending';
            """;
        command.Parameters.AddWithValue("$id", request.Id.ToString("D"));
        command.Parameters.AddWithValue("$status", request.Status.ToString());
        command.Parameters.AddWithValue("$serial", (object?)credential?.SerialNumber.ToString("D") ?? DBNull.Value);
        command.Parameters.AddWithValue("$issued", (object?)credential?.IssuedAt.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$reason", (object?)request.RejectionReason?.ToString() ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }
}
