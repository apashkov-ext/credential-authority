using CredentialAuthority.App.Features.GetCredential;
using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.Infra.Features.GetCredential;

public static class Module
{
    public static void AddGetCredentialFeatureInfra(this IServiceCollection services)
    {
        services.AddTransient<IFindCredential, SqliteFindCredential>();
    }
}
