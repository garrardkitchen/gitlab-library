using Garrard.GitLab.Http;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for the DI helper types in <see cref="Garrard.GitLab.Http"/>.
/// </summary>
public class HttpClientFactoryTests
{
    [Fact]
    public void DefaultGitLabHttpClientFactory_CreateClient_SetsAuthorizationHeader()
    {
        var factory = new DefaultGitLabHttpClientFactory();
        var client = factory.CreateClient("my-secret-token");

        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal("my-secret-token", client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public void DefaultGitLabHttpClientFactory_CreateClient_DifferentPats_ReturnDistinctClients()
    {
        var factory = new DefaultGitLabHttpClientFactory();
        var client1 = factory.CreateClient("token-a");
        var client2 = factory.CreateClient("token-b");

        Assert.NotEqual(
            client1.DefaultRequestHeaders.Authorization!.Parameter,
            client2.DefaultRequestHeaders.Authorization!.Parameter);
    }
}
