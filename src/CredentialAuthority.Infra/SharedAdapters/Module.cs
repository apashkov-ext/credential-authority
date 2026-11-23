using CredentialAuthority.App.SharedPorts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CredentialAuthority.Infra.SharedAdapters;

public static class Module
{
    public static void AddSharedAdapters(this IServiceCollection services)
    {
        services.TryAddTransient<IFindRequestById, SqliteFindRequestById>();
    }
}
