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

// TLS is terminated outside the container; only redirect when an HTTPS port is configured.
if (!string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORTS"]) || app.Environment.IsDevelopment())
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
