namespace Garrard.GitLab.Library.DTOs;

/// <summary>
/// Represents a summary of a GitLab project.
/// </summary>
public class ProjectSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string WebUrl { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public int VariableCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
    public bool IsMarkedForDeletion { get; init; }
}
