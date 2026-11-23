using System.ComponentModel.DataAnnotations;

namespace CredentialAuthority.Infra.Integrations.Sqlite;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string Path { get; set; } = "data/credentials.db";
}
