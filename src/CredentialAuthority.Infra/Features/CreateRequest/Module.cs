using CredentialAuthority.App.Features.CreateRequest;
using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.Infra.Features.CreateRequest;

public static class Module
{
    public static void AddCreateRequestFeatureInfra(this IServiceCollection services)
    {
        services.AddTransient<ITryCreateRequest, SqliteTryCreateRequest>();
    }
}
