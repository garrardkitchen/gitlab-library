using System.Net.Http.Headers;
using Garrard.GitLab.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for the DI helper types in <see cref="Garrard.GitLab.Http"/>.
/// </summary>
public class HttpClientFactoryTests
{
    [Fact]
    public void DefaultGitLabHttpClientFactory_CreateClient_DelegatesToNamedInnerFactory()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory
            .Setup(f => f.CreateClient(DefaultGitLabHttpClientFactory.ClientName))
            .Returns(new HttpClient());

        var factory = new DefaultGitLabHttpClientFactory(mockFactory.Object);
        _ = factory.CreateClient();

        mockFactory.Verify(f => f.CreateClient(DefaultGitLabHttpClientFactory.ClientName), Times.Once);
    }

    [Fact]
    public void AddGarrardGitLab_RegistersIGitLabHttpClientFactory()
    {
        var services = new ServiceCollection();
        services.AddGarrardGitLab(opts =>
        {
            opts.Pat = "test-pat";
            opts.Domain = "gitlab.example.com";
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IGitLabHttpClientFactory>();

        Assert.IsType<DefaultGitLabHttpClientFactory>(factory);
    }

    [Fact]
    public void AddGarrardGitLab_NamedHttpClient_HasBearerAuthAndBaseAddress()
    {
        const string pat = "glpat-test-token";
        const string domain = "gitlab.mycompany.com";

        var services = new ServiceCollection();
        services.AddGarrardGitLab(opts =>
        {
            opts.Pat = pat;
            opts.Domain = domain;
        });

        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient(DefaultGitLabHttpClientFactory.ClientName);

        Assert.Equal(new Uri($"https://{domain}/api/v4/"), client.BaseAddress);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal(pat, client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public void AddGarrardGitLab_WithoutOptions_StillRegistersServices()
    {
        // When no configureOptions delegate is provided, options should come from
        // the host configuration (env vars / appsettings), not from this call.
        var services = new ServiceCollection();
        services.AddGarrardGitLab(); // no delegate — options resolved from config at runtime

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IGitLabHttpClientFactory>();

        Assert.IsType<DefaultGitLabHttpClientFactory>(factory);
    }
}
