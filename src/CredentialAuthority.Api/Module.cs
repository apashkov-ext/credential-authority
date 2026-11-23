using System.Text.Json.Serialization;
using CredentialAuthority.Api.Serialization;
using CredentialAuthority.Infra.SharedAdapters;

namespace CredentialAuthority.Api;

internal static class Module
{
    public static void AddAdapters(this WebApplicationBuilder builder)
    {
        builder.Services.AddSharedAdapters();
    }

    public static void ConfigureApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
        });
    }
}
