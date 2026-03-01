namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// Represents a summary of a GitLab group.
/// </summary>
public class GroupSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string WebUrl { get; init; } = string.Empty;
    public int SubgroupCount { get; init; }
    public int ProjectCount { get; init; }
    public bool IsMarkedForDeletion { get; init; }
    public int? ParentId { get; init; }
}
