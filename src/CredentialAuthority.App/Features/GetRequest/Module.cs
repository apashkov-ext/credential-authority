using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.App.Features.GetRequest;

public static class Module
{
    public static void AddGetRequestFeatureApp(this IServiceCollection services)
    {
        services.AddTransient<IGetRequestUseCase, GetRequestUseCase>();
    }
}
