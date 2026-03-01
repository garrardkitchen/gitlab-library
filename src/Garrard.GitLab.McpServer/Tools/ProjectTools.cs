using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="Garrard.GitLab.ProjectOperations"/>.</summary>
[McpServerToolType]
public static class ProjectTools
{
    [McpServerTool(Name = "gitlab_get_projects_in_group"), Description("Gets all projects within a GitLab group.")]
    public static async Task<string> GetProjectsInGroup(
        IConfiguration config,
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Whether to include projects from subgroups (default: true).")] bool includeSubgroups = true,
        [Description("Order by field: id, name, path, created_at, updated_at, last_activity_at (default: name).")] string orderBy = "name",
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await ProjectOperations.GetProjectsInGroup(groupIdOrName, resolvedPat, resolvedDomain, includeSubgroups, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_variables"), Description("Gets all variables for a GitLab project.")]
    public static async Task<string> GetProjectVariables(
        IConfiguration config,
        [Description("The ID of the project.")] string projectId,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await ProjectOperations.GetProjectVariables(projectId, resolvedPat, resolvedDomain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_variable"), Description("Gets a specific variable from a GitLab project.")]
    public static async Task<string> GetProjectVariable(
        IConfiguration config,
        [Description("The ID of the project.")] string projectId,
        [Description("The key of the variable to retrieve.")] string variableKey,
        [Description("Environment scope filter (default: *).")] string? environmentScope = "*",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await ProjectOperations.GetProjectVariable(projectId, variableKey, resolvedPat, resolvedDomain, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_or_update_project_variable"), Description("Creates or updates a variable in a GitLab project.")]
    public static async Task<string> CreateOrUpdateProjectVariable(
        IConfiguration config,
        [Description("The ID of the project.")] string projectId,
        [Description("The variable key.")] string variableKey,
        [Description("The variable value.")] string variableValue,
        [Description("Variable type: env_var or file (default: env_var).")] string variableType = "env_var",
        [Description("Whether the variable is protected (default: false).")] bool isProtected = false,
        [Description("Whether the variable is masked (default: false).")] bool isMasked = false,
        [Description("Environment scope (default: *).")] string? environmentScope = "*",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await ProjectOperations.CreateOrUpdateProjectVariable(
            projectId, variableKey, variableValue, resolvedPat, resolvedDomain,
            variableType, isProtected, isMasked, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_delete_project_variable"), Description("Deletes a variable from a GitLab project.")]
    public static async Task<string> DeleteProjectVariable(
        IConfiguration config,
        [Description("The ID of the project.")] string projectId,
        [Description("The key of the variable to delete.")] string variableKey,
        [Description("Environment scope filter (default: *).")] string? environmentScope = "*",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await ProjectOperations.DeleteProjectVariable(projectId, variableKey, resolvedPat, resolvedDomain, environmentScope);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_project"), Description("Creates a new GitLab project inside an optional group.")]
    public static async Task<string> CreateProject(
        IConfiguration config,
        [Description("The name of the project to create.")] string name,
        [Description("Optional parent group ID to place the project in.")] int? parentGroupId = null,
        [Description("Whether to enable shared instance runners (optional).")] bool? enableInstanceRunners = null,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await ProjectOperations.CreateGitLabProject(name, resolvedPat, resolvedDomain, parentGroupId, enableInstanceRunners);
        return ToolHelper.Serialize(result);
    }
}
