using CredentialAuthority.Api.Workers.DatabaseInitializationWorker;
using CredentialAuthority.Api.Workers.IssuanceWorker;

namespace CredentialAuthority.Api.Workers;

internal static class Module
{
    public static void AddWorkers(this WebApplicationBuilder builder)
    {
        // Database initialization must finish before the issuance worker and HTTP server start.
        builder.Services.Configure<HostOptions>(options => options.ServicesStartConcurrently = false);
        builder.AddDatabaseInitializationWorker();
        builder.AddIssuanceWorker();
    }
}
