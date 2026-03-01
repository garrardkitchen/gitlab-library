namespace Garrard.GitLab.Http;

/// <summary>
/// Abstraction for creating authenticated <see cref="HttpClient"/> instances for GitLab API calls.
/// The implementation uses a pooled named client configured from <see cref="GitLabOptions"/>,
/// so no PAT needs to be passed at call time.
/// </summary>
public interface IGitLabHttpClientFactory
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with the Bearer PAT and base address
    /// from <see cref="GitLabOptions"/>. The underlying connection pool is managed by
    /// <see cref="IHttpClientFactory"/>.
    /// </summary>
    HttpClient CreateClient();
}
