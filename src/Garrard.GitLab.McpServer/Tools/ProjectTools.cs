using System.ComponentModel;
using Garrard.GitLab;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="ProjectOperations"/>.</summary>
[McpServerToolType]
public sealed class ProjectTools(IOptions<GitLabOptions> options)
{
    private readonly GitLabOptions _opts = options.Value;

    [McpServerTool(Name = "gitlab_get_projects_in_group"), Description("Gets all projects within a GitLab group.")]
    public async Task<string> GetProjectsInGroup(
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Whether to include projects from subgroups (default: true).")] bool includeSubgroups = true,
        [Description("Order by field: id, name, path, created_at, updated_at, last_activity_at (default: name).")] string orderBy = "name",
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await ProjectOperations.GetProjectsInGroup(groupIdOrName, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, includeSubgroups, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_variables"), Description("Gets all variables for a GitLab project.")]
    public async Task<string> GetProjectVariables(
        [Description("The ID of the project.")] string projectId,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await ProjectOperations.GetProjectVariables(projectId, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_variable"), Description("Gets a specific variable from a GitLab project.")]
    public async Task<string> GetProjectVariable(
        [Description("The ID of the project.")] string projectId,
        [Description("The key of the variable to retrieve.")] string variableKey,
        [Description("Environment scope filter (default: *).")] string? environmentScope = "*",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await ProjectOperations.GetProjectVariable(projectId, variableKey, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_or_update_project_variable"), Description("Creates or updates a variable in a GitLab project.")]
    public async Task<string> CreateOrUpdateProjectVariable(
        [Description("The ID of the project.")] string projectId,
        [Description("The variable key.")] string variableKey,
        [Description("The variable value.")] string variableValue,
        [Description("Variable type: env_var or file (default: env_var).")] string variableType = "env_var",
        [Description("Whether the variable is protected (default: false).")] bool isProtected = false,
        [Description("Whether the variable is masked (default: false).")] bool isMasked = false,
        [Description("Environment scope (default: *).")] string? environmentScope = "*",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await ProjectOperations.CreateOrUpdateProjectVariable(
            projectId, variableKey, variableValue, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain,
            variableType, isProtected, isMasked, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_delete_project_variable"), Description("Deletes a variable from a GitLab project.")]
    public async Task<string> DeleteProjectVariable(
        [Description("The ID of the project.")] string projectId,
        [Description("The key of the variable to delete.")] string variableKey,
        [Description("Environment scope filter (default: *).")] string? environmentScope = "*",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await ProjectOperations.DeleteProjectVariable(projectId, variableKey, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_project"), Description("Creates a new GitLab project inside an optional group.")]
    public async Task<string> CreateProject(
        [Description("The name of the project to create.")] string name,
        [Description("Optional parent group ID to place the project in.")] int? parentGroupId = null,
        [Description("Whether to enable shared instance runners (optional).")] bool? enableInstanceRunners = null,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await ProjectOperations.CreateGitLabProject(name, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, parentGroupId, enableInstanceRunners);
        return ToolHelper.Serialize(result);
    }
}
