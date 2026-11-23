using CredentialAuthority.App.Features.GetRequest;

namespace CredentialAuthority.Api.Features.GetRequest;

internal static class Module
{
    public static void AddGetRequestFeature(this WebApplicationBuilder builder)
    {
        builder.Services.AddGetRequestFeatureApp();
    }

    public static void MapGetRequestFeature(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/requests/{id:guid}", Handle);
    }

    private static async Task<IResult> Handle(Guid id, IGetRequestUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.Execute(id, ct);
        return result.Match<IResult>(
            request => Results.Ok(RequestResponse.From(request)),
            _ => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Request not found"));
    }
}
