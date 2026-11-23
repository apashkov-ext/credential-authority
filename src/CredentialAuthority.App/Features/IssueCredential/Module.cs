using Microsoft.Extensions.DependencyInjection;

namespace CredentialAuthority.App.Features.IssueCredential;

public static class Module
{
    public static void AddIssueCredentialFeatureApp(this IServiceCollection services)
    {
        services.AddTransient<IIssueCredentialUseCase, IssueCredentialUseCase>();
        services.AddTransient<IProcessPendingRequestsUseCase, ProcessPendingRequestsUseCase>();
    }
}
