using CredentialAuthority.App.Features.GetCredential;
using CredentialAuthority.Infra.Features.GetCredential;

namespace CredentialAuthority.Api.Features.GetCredential;

internal static class Module
{
    public static void AddGetCredentialFeature(this WebApplicationBuilder builder)
    {
        builder.Services.AddGetCredentialFeatureApp();
        builder.Services.AddGetCredentialFeatureInfra();
    }

    public static void MapGetCredentialFeature(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/credentials/{serialNumber:guid}", Handle);
    }

    private static async Task<IResult> Handle(Guid serialNumber, IGetCredentialUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.Execute(serialNumber, ct);
        return result.Match<IResult>(
            credential => Results.Ok(CredentialResponse.From(credential)),
            _ => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Credential not found"));
    }
}
