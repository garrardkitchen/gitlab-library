using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// A minimal project reference returned by the legacy GitLab project-create endpoint.
/// </summary>
public class GitLabProjectReference
{
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("http_url_to_repo")]
    public string HttpUrlToRepo { get; init; } = string.Empty;

    [JsonPropertyName("path_with_namespace")]
    public string PathWithNamespace { get; init; } = string.Empty;
}
