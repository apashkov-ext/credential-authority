using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Api.Integrations;

internal static class Module
{
    public static void AddIntegrations(this WebApplicationBuilder builder)
    {
        builder.Services.AddSqliteIntegration(builder.Configuration, builder.Environment.ContentRootPath);
    }
}
