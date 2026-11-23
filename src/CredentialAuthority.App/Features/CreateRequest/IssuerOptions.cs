using System.ComponentModel.DataAnnotations;

namespace CredentialAuthority.App.Features.CreateRequest;

public sealed class IssuerOptions
{
    public const string SectionName = "Issuer";

    [Required]
    public string Name { get; set; } = "Demo Authority";
}
