using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.Requests;

internal sealed class CreateProjectRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("namespace_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NamespaceId { get; init; }

    [JsonPropertyName("shared_runners_enabled")]
    public bool SharedRunnersEnabled { get; init; }
}
