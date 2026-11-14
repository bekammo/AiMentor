using AiMentor.Web.Components;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Redirect to HTTPS only when an HTTPS endpoint exists (launch profile "https" or HTTPS_PORTS).
// The container listens on plain HTTP; TLS, if any, is terminated in front of it.
var hasHttpsEndpoint = !string.IsNullOrEmpty(app.Configuration["HTTPS_PORTS"])
    || (app.Configuration["URLS"] ?? string.Empty).Contains("https://", StringComparison.OrdinalIgnoreCase);
if (hasHttpsEndpoint)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

// /health: every registered check (database checks are added in P1-T1). /alive: process liveness only.
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
