using System.Globalization;
using CredentialAuthority.App.Core.Models;
using Microsoft.Data.Sqlite;

namespace CredentialAuthority.Infra.SharedAdapters;

internal static class SqliteRequestReader
{
    public static IssuanceRequest Read(SqliteDataReader reader)
    {
        var request = new IssuanceRequest(
            Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            OwnerName.Create(reader.GetString(reader.GetOrdinal("owner_name"))),
            reader.GetString(reader.GetOrdinal("issuer")),
            DateTimeOffset.ParseExact(reader.GetString(reader.GetOrdinal("valid_until")),
                "O", CultureInfo.InvariantCulture));

        return reader.GetString(reader.GetOrdinal("status")) switch
        {
            "Pending" => request,
            "Issued" => request.Issue(Guid.Parse(reader.GetString(reader.GetOrdinal("serial_number")))),
            "Rejected" => request.Reject(Enum.Parse<RejectionReason>(reader.GetString(reader.GetOrdinal("rejection_reason")))),
            var status => throw new InvalidDataException($"Unknown stored request status: {status}")
        };
    }
}
