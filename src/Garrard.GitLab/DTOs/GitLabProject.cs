using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// Represents a GitLab namespace (group or user).
/// </summary>
public class GitLabNamespace
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("full_path")]
    public string FullPath { get; init; } = string.Empty;
}

/// <summary>
/// Represents a GitLab project with its full details.
/// </summary>
public class GitLabProject
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("web_url")]
    public string WebUrl { get; init; } = string.Empty;

    [JsonPropertyName("ssh_url_to_repo")]
    public string SshUrlToRepo { get; init; } = string.Empty;

    [JsonPropertyName("http_url_to_repo")]
    public string HttpUrlToRepo { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("namespace")]
    public GitLabNamespace Namespace { get; init; } = new();

    /// <summary>Gets the ID of the group that the project belongs to.</summary>
    public int GroupId => Namespace?.Id ?? 0;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("last_activity_at")]
    public DateTime LastActivityAt { get; init; }

    [JsonPropertyName("marked_for_deletion_at")]
    public string? MarkedForDeletionAt { get; init; }

    /// <summary>Indicates whether the project is marked for deletion.</summary>
    public bool IsMarkedForDeletion => !string.IsNullOrEmpty(MarkedForDeletionAt);
}
