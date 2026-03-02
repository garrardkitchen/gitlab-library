using System.Text.Json.Serialization;

namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// Wraps a paginated API response with metadata about the current page.
/// </summary>
public sealed class PagedResult<T>
{
    [JsonPropertyName("items")]
    public T[] Items { get; init; } = [];

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("total_items")]
    public int TotalItems { get; init; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; init; }
}
