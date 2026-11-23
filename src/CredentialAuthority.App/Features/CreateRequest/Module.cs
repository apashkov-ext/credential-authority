using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.App.Features.CreateRequest;

public static class Module
{
    public static void AddCreateRequestFeatureApp(this IServiceCollection services)
    {
        services.AddTransient<ICreateRequestUseCase, CreateRequestUseCase>();
    }
}
