using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// Represents a GitLab CI/CD variable (used for both project and group variables).
/// </summary>
public class GitLabVariable
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("variable_type")]
    public string VariableType { get; init; } = string.Empty;

    [JsonPropertyName("protected")]
    public bool Protected { get; init; }

    [JsonPropertyName("masked")]
    public bool Masked { get; init; }

    [JsonPropertyName("environment_scope")]
    public string EnvironmentScope { get; init; } = string.Empty;
}
