using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="Garrard.GitLab.GroupOperations"/>.</summary>
[McpServerToolType]
public static class GroupTools
{
    [McpServerTool(Name = "gitlab_get_subgroups"), Description("Gets all subgroups beneath a specified GitLab group.")]
    public static async Task<string> GetSubgroups(
        IConfiguration config,
        [Description("The ID or name of the parent group.")] string groupIdOrName,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GroupOperations.GetSubgroups(groupIdOrName, resolvedPat, resolvedDomain, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_find_groups"), Description("Finds GitLab groups by exact name or ID.")]
    public static async Task<string> FindGroups(
        IConfiguration config,
        [Description("The exact name or ID of the group to find.")] string nameOrId,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GroupOperations.FindGroups(nameOrId, resolvedPat, resolvedDomain, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_search_groups"), Description("Searches for GitLab groups using a wildcard pattern.")]
    public static async Task<string> SearchGroups(
        IConfiguration config,
        [Description("The search pattern to match groups (supports partial matches).")] string searchPattern,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name",
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GroupOperations.SearchGroups(searchPattern, resolvedPat, resolvedDomain, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_group"), Description("Creates a new GitLab group or subgroup.")]
    public static async Task<string> CreateGroup(
        IConfiguration config,
        [Description("The name of the group to create.")] string name,
        [Description("Optional parent group ID to create a subgroup.")] int? parentId = null,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GroupOperations.CreateGitLabGroup(name, resolvedPat, resolvedDomain, parentId);
        return ToolHelper.Serialize(result);
    }
}
