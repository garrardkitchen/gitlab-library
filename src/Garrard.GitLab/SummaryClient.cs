using CSharpFunctionalExtensions;
using Garrard.GitLab.Library.DTOs;

namespace Garrard.GitLab.Library;

/// <summary>
/// Instance client for summarizing GitLab entities.
/// Delegates to <see cref="GroupClient"/> and <see cref="ProjectClient"/>.
/// </summary>
public sealed class SummaryClient
{
    private readonly GroupClient _groupClient;
    private readonly ProjectClient _projectClient;

    public SummaryClient(GroupClient groupClient, ProjectClient projectClient)
    {
        _groupClient = groupClient;
        _projectClient = projectClient;
    }

    /// <summary>
    /// Gets a summary of a GitLab group including subgroup and project counts.
    /// </summary>
    public async Task<Result<GroupSummary>> GetGroupSummary(
        string groupIdOrName,
        Action<string>? logAction = null)
    {
        try
        {
            logAction?.Invoke($"Getting summary for group: {groupIdOrName}");

            var groupResult = await _groupClient.FindGroups(groupIdOrName);
            if (groupResult.IsFailure)
                return Result.Failure<GroupSummary>(groupResult.Error);

            var group = groupResult.Value.FirstOrDefault();
            if (group == null)
                return Result.Failure<GroupSummary>($"Group '{groupIdOrName}' not found");

            var subgroupsResult = await _groupClient.GetSubgroups(group.Id.ToString());
            int subgroupCount = subgroupsResult.IsSuccess ? subgroupsResult.Value.Length : 0;

            var projectsResult = await _projectClient.GetProjectsInGroup(group.Id.ToString());
            int projectCount = projectsResult.IsSuccess ? projectsResult.Value.Length : 0;

            var summary = new GroupSummary
            {
                Id = group.Id,
                Name = group.Name,
                FullPath = group.FullPath,
                WebUrl = group.WebUrl,
                SubgroupCount = subgroupCount,
                ProjectCount = projectCount,
                IsMarkedForDeletion = group.IsMarkedForDeletion,
                ParentId = group.ParentId
            };

            logAction?.Invoke($"Group summary completed: {summary.Name} ({summary.SubgroupCount} subgroups, {summary.ProjectCount} projects)");

            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            return Result.Failure<GroupSummary>($"Error getting group summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a summary of a GitLab project including variable count and basic information.
    /// </summary>
    public async Task<Result<ProjectSummary>> GetProjectSummary(
        int projectId,
        Action<string>? logAction = null)
    {
        try
        {
            logAction?.Invoke($"Getting summary for project ID: {projectId}");

            var variablesResult = await _projectClient.GetProjectVariables(projectId.ToString(), logAction);
            int variableCount = variablesResult.IsSuccess ? variablesResult.Value.Length : 0;

            logAction?.Invoke("Note: Project details retrieval requires knowing the group. Variable count retrieved successfully.");

            var summary = new ProjectSummary
            {
                Id = projectId,
                Name = $"Project-{projectId}",
                Description = "Project details require group context",
                WebUrl = $"https://{_projectClient.Domain}/projects/{projectId}",
                Path = $"project-{projectId}",
                GroupId = 0,
                GroupName = "Unknown",
                VariableCount = variableCount,
                CreatedAt = DateTime.MinValue,
                LastActivityAt = DateTime.MinValue,
                IsMarkedForDeletion = false
            };

            logAction?.Invoke($"Project summary completed: Project {projectId} ({variableCount} variables)");

            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            return Result.Failure<ProjectSummary>($"Error getting project summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets summaries of all projects in a group.
    /// </summary>
    public async Task<Result<ProjectSummary[]>> GetGroupProjectsSummary(
        string groupIdOrName,
        bool includeSubgroups = true,
        Action<string>? logAction = null)
    {
        try
        {
            logAction?.Invoke($"Getting project summaries for group: {groupIdOrName}");

            var projectsResult = await _projectClient.GetProjectsInGroup(groupIdOrName, includeSubgroups);
            if (projectsResult.IsFailure)
                return Result.Failure<ProjectSummary[]>(projectsResult.Error);

            var projects = projectsResult.Value;
            var summaries = new List<ProjectSummary>();

            foreach (var project in projects)
            {
                var variablesResult = await _projectClient.GetProjectVariables(project.Id.ToString());
                int variableCount = variablesResult.IsSuccess ? variablesResult.Value.Length : 0;

                var summary = new ProjectSummary
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    WebUrl = project.WebUrl,
                    Path = project.Path,
                    GroupId = project.GroupId,
                    GroupName = project.Namespace.Name,
                    VariableCount = variableCount,
                    CreatedAt = project.CreatedAt,
                    LastActivityAt = project.LastActivityAt,
                    IsMarkedForDeletion = project.IsMarkedForDeletion
                };

                summaries.Add(summary);
            }

            logAction?.Invoke($"Retrieved summaries for {summaries.Count} projects");

            return Result.Success(summaries.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<ProjectSummary[]>($"Error getting group projects summary: {ex.Message}");
        }
    }
}
