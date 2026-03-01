using System.ComponentModel;
using Garrard.GitLab.Library;
using Garrard.GitLab.Library.Enums;
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
        [Description("Environment scope (default: *).")] string? environmentScope = "*",
        [Description("Whether the variable value is hidden after creation (default: true).")] bool isHidden = true)
    {
        var result = await projectClient.CreateOrUpdateProjectVariable(
            projectId, variableKey, variableValue,
            variableType, isProtected, isMasked, environmentScope, isHidden);
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

    [McpServerTool(Name = "gitlab_create_project_access_token"), Description("Creates a project access token with configurable scopes and access level.")]
    public async Task<string> CreateProjectAccessToken(
        [Description("The ID of the project.")] string projectId,
        [Description("The name of the access token (default: GL_TOKEN).")] string name = "GL_TOKEN",
        [Description("Comma-separated scopes: api, read_api, read_repository, write_repository, read_registry, write_registry, read_package_registry, write_package_registry, create_runner, manage_runner, ai_features, k8s_proxy (default: write_repository).")] string scopes = "write_repository",
        [Description("Access level: 10=Guest, 20=Reporter, 30=Developer, 40=Maintainer, 50=Owner (default: 40).")] int accessLevel = 40,
        [Description("Expiry date in YYYY-MM-DD format (default: one year from today).")] string? expiresAt = null)
    {
        var scopeFlags = ParseScopes(scopes);
        DateOnly? expiry = null;
        if (expiresAt is not null)
        {
            if (!DateOnly.TryParse(expiresAt, out var parsedExpiry))
                return $"{{\"error\": \"Invalid expiresAt date '{expiresAt}'. Use YYYY-MM-DD format.\"}}";
            expiry = parsedExpiry;
        }
        var result = await projectClient.CreateProjectAccessToken(
            projectId, name, scopeFlags, (AccessLevel)accessLevel, expiry);
        return ToolHelper.Serialize(result);
    }

    private static ProjectAccessTokenScope ParseScopes(string scopes)
    {
        var flags = ProjectAccessTokenScope.ReadRepository; // safe fallback
        flags = 0;
        foreach (var scope in scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            flags |= scope.ToLowerInvariant() switch
            {
                "api"                    => ProjectAccessTokenScope.Api,
                "read_api"               => ProjectAccessTokenScope.ReadApi,
                "read_repository"        => ProjectAccessTokenScope.ReadRepository,
                "write_repository"       => ProjectAccessTokenScope.WriteRepository,
                "read_registry"          => ProjectAccessTokenScope.ReadRegistry,
                "write_registry"         => ProjectAccessTokenScope.WriteRegistry,
                "read_package_registry"  => ProjectAccessTokenScope.ReadPackageRegistry,
                "write_package_registry" => ProjectAccessTokenScope.WritePackageRegistry,
                "create_runner"          => ProjectAccessTokenScope.CreateRunner,
                "manage_runner"          => ProjectAccessTokenScope.ManageRunner,
                "ai_features"            => ProjectAccessTokenScope.AiFeatures,
                "k8s_proxy"              => ProjectAccessTokenScope.K8sProxy,
                "read_observability"     => ProjectAccessTokenScope.ReadObservability,
                "write_observability"    => ProjectAccessTokenScope.WriteObservability,
                _                        => 0
            };
        }
        return flags == 0 ? ProjectAccessTokenScope.WriteRepository : flags;
    }
}
