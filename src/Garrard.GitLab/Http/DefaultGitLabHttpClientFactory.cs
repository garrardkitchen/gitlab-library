namespace Garrard.GitLab.Library.Http;

/// <summary>
/// Default implementation of <see cref="IGitLabHttpClientFactory"/> that delegates to
/// <see cref="IHttpClientFactory"/> for connection-pooled, PAT-authenticated clients.
/// The named client (<see cref="ClientName"/>) is registered by
/// <see cref="ServiceCollectionExtensions.AddGarrardGitLab"/>.
/// </summary>
public sealed class DefaultGitLabHttpClientFactory : IGitLabHttpClientFactory
{
    /// <summary>Name of the named HTTP client registered with <see cref="IHttpClientFactory"/>.</summary>
    public const string ClientName = "gitlab";

    private readonly IHttpClientFactory _httpClientFactory;

    /// <param name="httpClientFactory">The ASP.NET Core / Microsoft.Extensions.Http factory.</param>
    public DefaultGitLabHttpClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public HttpClient CreateClient() => _httpClientFactory.CreateClient(ClientName);
}
