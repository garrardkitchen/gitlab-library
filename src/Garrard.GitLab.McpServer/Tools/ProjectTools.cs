using System.ComponentModel;
using Garrard.GitLab.Library;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="ProjectClient"/>.</summary>
[McpServerToolType]
public sealed class ProjectTools(ProjectClient projectClient)
{
    [McpServerTool(Name = "gitlab_get_projects_in_group"), Description("Gets all projects within a GitLab group.")]
    public async Task<string> GetProjectsInGroup(
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Whether to include projects from subgroups (default: true).")] bool includeSubgroups = true,
        [Description("Order by field: id, name, path, created_at, updated_at, last_activity_at (default: name).")] string orderBy = "name",
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc")
    {
        var result = await projectClient.GetProjectsInGroup(groupIdOrName, includeSubgroups, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_variables"), Description("Gets all variables for a GitLab project.")]
    public async Task<string> GetProjectVariables(
        [Description("The ID of the project.")] string projectId)
    {
        var result = await projectClient.GetProjectVariables(projectId);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_variable"), Description("Gets a specific variable from a GitLab project.")]
    public async Task<string> GetProjectVariable(
        [Description("The ID of the project.")] string projectId,
        [Description("The key of the variable to retrieve.")] string variableKey,
        [Description("Environment scope filter (default: *).")] string? environmentScope = "*")
    {
        var result = await projectClient.GetProjectVariable(projectId, variableKey, environmentScope);
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
        [Description("Environment scope (default: *).")] string? environmentScope = "*")
    {
        var result = await projectClient.CreateOrUpdateProjectVariable(
            projectId, variableKey, variableValue,
            variableType, isProtected, isMasked, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_delete_project_variable"), Description("Deletes a variable from a GitLab project.")]
    public async Task<string> DeleteProjectVariable(
        [Description("The ID of the project.")] string projectId,
        [Description("The key of the variable to delete.")] string variableKey,
        [Description("Environment scope filter (default: *).")] string? environmentScope = "*")
    {
        var result = await projectClient.DeleteProjectVariable(projectId, variableKey, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_project"), Description("Creates a new GitLab project inside an optional group.")]
    public async Task<string> CreateProject(
        [Description("The name of the project to create.")] string name,
        [Description("Optional parent group ID to place the project in.")] int? parentGroupId = null,
        [Description("Whether to enable shared instance runners (optional).")] bool? enableInstanceRunners = null)
    {
        var result = await projectClient.CreateGitLabProject(name, parentGroupId, enableInstanceRunners);
        return ToolHelper.Serialize(result);
    }
}
