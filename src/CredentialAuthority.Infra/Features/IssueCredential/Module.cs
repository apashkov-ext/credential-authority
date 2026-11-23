using CredentialAuthority.App.Features.IssueCredential;
using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.Infra.Features.IssueCredential;

public static class Module
{
    public static void AddIssueCredentialFeatureInfra(this IServiceCollection services)
    {
        services.AddTransient<ICompleteRequest, SqliteCompleteRequest>();
        services.AddTransient<IGetPendingRequestIds, SqliteGetPendingRequestIds>();
    }
}
