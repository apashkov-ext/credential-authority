using CredentialAuthority.App.Features.IssueCredential;
using CredentialAuthority.Infra.Features.IssueCredential;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CredentialAuthority.Api.Workers.IssuanceWorker;

internal static class Module
{
    public static void AddIssuanceWorker(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddIssueCredentialFeatureApp();
        builder.Services.AddIssueCredentialFeatureInfra();
        builder.Services.AddHostedService<IssuanceWorker>();
    }
}
