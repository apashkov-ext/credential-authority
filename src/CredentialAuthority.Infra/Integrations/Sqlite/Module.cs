using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CredentialAuthority.Infra.Integrations.Sqlite;

public static class Module
{
    public static void AddSqliteIntegration(this IServiceCollection services,
        IConfiguration configuration, string contentRoot)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return new SqliteDatabase(System.IO.Path.GetFullPath(options.Path, contentRoot));
        });
    }
}
