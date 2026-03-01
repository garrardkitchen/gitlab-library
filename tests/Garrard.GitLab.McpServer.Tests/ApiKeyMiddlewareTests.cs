using System.Net;
using System.Net.Http.Headers;
using Garrard.GitLab.McpServer.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Garrard.GitLab.McpServer.Tests;

/// <summary>
/// Unit tests for <see cref="ApiKeyMiddleware"/> using a lightweight in-process test server.
/// </summary>
public class ApiKeyMiddlewareTests
{
    private static async Task<HttpClient> BuildTestClient(Dictionary<string, string?> config)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration((_, cfg) =>
                    cfg.AddInMemoryCollection(config));
                webBuilder.ConfigureServices(services => services.AddRouting());
                webBuilder.Configure(app =>
                {
                    app.UseMiddleware<ApiKeyMiddleware>();
                    app.Run(ctx =>
                    {
                        ctx.Response.StatusCode = 200;
                        return ctx.Response.WriteAsync("OK");
                    });
                });
            })
            .StartAsync();

        return host.GetTestClient();
    }

    [Fact]
    public async Task NoApiKeyConfigured_AllowsRequestThrough()
    {
        var client = await BuildTestClient(new Dictionary<string, string?>());

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyConfigured_MissingAuthHeader_Returns401()
    {
        var client = await BuildTestClient(new() { ["MCP_API_KEY"] = "secret-key-123" });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyConfigured_WrongKey_Returns401()
    {
        var client = await BuildTestClient(new() { ["MCP_API_KEY"] = "secret-key-123" });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-key");

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyConfigured_CorrectKey_Returns200()
    {
        const string key = "my-valid-api-key";
        var client = await BuildTestClient(new() { ["MCP_API_KEY"] = key });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyConfigured_MissingBearerPrefix_Returns401()
    {
        var client = await BuildTestClient(new() { ["MCP_API_KEY"] = "my-key" });
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "my-key"); // no "Bearer" prefix

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiKeyConfigured_EmptyAuthHeader_Returns401()
    {
        var client = await BuildTestClient(new() { ["MCP_API_KEY"] = "my-key" });
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "");

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
