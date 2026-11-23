using CredentialAuthority.Api.Features.CreateRequest;
using CredentialAuthority.Api.Features.GetCredential;
using CredentialAuthority.Api.Features.GetRequest;

namespace CredentialAuthority.Api.Features;

internal static class Module
{
    public static void AddFeatures(this WebApplicationBuilder builder)
    {
        builder.AddCreateRequestFeature();
        builder.AddGetRequestFeature();
        builder.AddGetCredentialFeature();
    }

    public static void MapFeatures(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCreateRequestFeature();
        endpoints.MapGetRequestFeature();
        endpoints.MapGetCredentialFeature();
    }
}
