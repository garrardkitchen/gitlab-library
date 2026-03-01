namespace Garrard.GitLab.Http;

/// <summary>
/// Abstraction for creating authenticated <see cref="HttpClient"/> instances for GitLab API calls.
/// </summary>
public interface IGitLabHttpClientFactory
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> configured with the provided GitLab PAT.
    /// </summary>
    HttpClient CreateClient(string pat);
}
