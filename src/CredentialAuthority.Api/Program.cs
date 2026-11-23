using CredentialAuthority.Api;
using CredentialAuthority.Api.Features;
using CredentialAuthority.Api.Integrations;
using CredentialAuthority.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApi();
builder.AddIntegrations();
builder.AddAdapters();
builder.AddFeatures();
builder.AddWorkers();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapFeatures();

await app.RunAsync();

public partial class Program;
