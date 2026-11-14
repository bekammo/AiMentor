using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiMentor.IntegrationTests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_endpoints_return_ok(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
