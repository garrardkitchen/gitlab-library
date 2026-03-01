using System.ComponentModel;
using Garrard.GitLab.Library;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="GroupClient"/>.</summary>
[McpServerToolType]
public sealed class GroupTools(GroupClient groupClient)
{
    [McpServerTool(Name = "gitlab_get_subgroups"), Description("Gets all subgroups beneath a specified GitLab group.")]
    public async Task<string> GetSubgroups(
        [Description("The ID or name of the parent group.")] string groupIdOrName,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name")
    {
        var result = await groupClient.GetSubgroups(groupIdOrName, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_find_groups"), Description("Finds GitLab groups by exact name or ID.")]
    public async Task<string> FindGroups(
        [Description("The exact name or ID of the group to find.")] string nameOrId,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name")
    {
        var result = await groupClient.FindGroups(nameOrId, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_search_groups"), Description("Searches for GitLab groups using a wildcard pattern.")]
    public async Task<string> SearchGroups(
        [Description("The search pattern to match groups (supports partial matches).")] string searchPattern,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name")
    {
        var result = await groupClient.SearchGroups(searchPattern, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_group"), Description("Creates a new GitLab group or subgroup.")]
    public async Task<string> CreateGroup(
        [Description("The name of the group to create.")] string name,
        [Description("Optional parent group ID to create a subgroup.")] int? parentId = null)
    {
        var result = await groupClient.CreateGitLabGroup(name, parentId);
        return ToolHelper.Serialize(result);
    }
}
