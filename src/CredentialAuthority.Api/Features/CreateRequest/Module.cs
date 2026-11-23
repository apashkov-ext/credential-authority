using CredentialAuthority.App.Features.CreateRequest;
using CredentialAuthority.Infra.Features.CreateRequest;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CredentialAuthority.Api.Features.CreateRequest;

internal static class Module
{
    public static void AddCreateRequestFeature(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddOptions<IssuerOptions>()
            .BindConfiguration(IssuerOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddCreateRequestFeatureApp();
        builder.Services.AddCreateRequestFeatureInfra();
    }

    public static void MapCreateRequestFeature(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/requests", Handle);
    }

    private static async Task<IResult> Handle(CreateRequestBody body, ICreateRequestUseCase useCase,
        CancellationToken ct)
    {
        var result = await useCase.Execute(body.OwnerName, body.ValidUntil, ct);
        return result.Match<IResult>(
            success => Results.Accepted($"/requests/{success.Id:D}", new CreateRequestResponse(success.Id)),
            invalid => Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request", detail: invalid.Error),
            _ => Results.Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Owner conflict", detail: "This owner already has a pending request or an issued credential."));
    }
}
