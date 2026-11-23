using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.App.Features.GetCredential;

public static class Module
{
    public static void AddGetCredentialFeatureApp(this IServiceCollection services)
    {
        services.AddTransient<IGetCredentialUseCase, GetCredentialUseCase>();
    }
}
