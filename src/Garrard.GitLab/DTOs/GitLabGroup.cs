using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// Represents a GitLab group.
/// </summary>
public class GitLabGroup
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("full_path")]
    public string FullPath { get; init; } = string.Empty;

    [JsonPropertyName("web_url")]
    public string WebUrl { get; init; } = string.Empty;

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; init; }

    [JsonPropertyName("has_subgroups")]
    public bool HasSubgroups { get; set; }

    [JsonPropertyName("marked_for_deletion_on")]
    public string? MarkedForDeletionOn { get; init; }

    /// <summary>
    /// Indicates whether the group is marked for deletion.
    /// </summary>
    public bool IsMarkedForDeletion => !string.IsNullOrEmpty(MarkedForDeletionOn);
}
