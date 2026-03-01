using System.Net.Http.Headers;

namespace Garrard.GitLab.Http;

/// <summary>
/// Default implementation of <see cref="IGitLabHttpClientFactory"/> that creates
/// a new <see cref="HttpClient"/> with the PAT set as a Bearer token.
/// </summary>
public sealed class DefaultGitLabHttpClientFactory : IGitLabHttpClientFactory
{
    /// <inheritdoc />
    public HttpClient CreateClient(string pat)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", pat);
        return client;
    }
}
