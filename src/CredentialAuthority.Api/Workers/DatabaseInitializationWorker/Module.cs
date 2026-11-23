namespace CredentialAuthority.Api.Workers.DatabaseInitializationWorker;

internal static class Module
{
    public static void AddDatabaseInitializationWorker(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<DatabaseInitializationWorker>();
    }
}
