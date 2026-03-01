using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.DTOs;

/// <summary>Represents a GitLab project access token returned from the API.</summary>
public sealed class GitLabProjectAccessToken
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("token")]
    public string? Token { get; init; }  // Only populated on creation

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; init; } = [];

    [JsonPropertyName("access_level")]
    public int AccessLevel { get; init; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; init; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("revoked")]
    public bool Revoked { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }
}
