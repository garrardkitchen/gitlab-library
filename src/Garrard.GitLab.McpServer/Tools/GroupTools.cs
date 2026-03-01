using System.ComponentModel;
using Garrard.GitLab;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="GroupOperations"/>.</summary>
[McpServerToolType]
public sealed class GroupTools(IOptions<GitLabOptions> options)
{
    private readonly GitLabOptions _opts = options.Value;

    [McpServerTool(Name = "gitlab_get_subgroups"), Description("Gets all subgroups beneath a specified GitLab group.")]
    public async Task<string> GetSubgroups(
        [Description("The ID or name of the parent group.")] string groupIdOrName,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name",
        [Description("Override GitLab domain (e.g. gitlab.com). Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GroupOperations.GetSubgroups(groupIdOrName, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_find_groups"), Description("Finds GitLab groups by exact name or ID.")]
    public async Task<string> FindGroups(
        [Description("The exact name or ID of the group to find.")] string nameOrId,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GroupOperations.FindGroups(nameOrId, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_search_groups"), Description("Searches for GitLab groups using a wildcard pattern.")]
    public async Task<string> SearchGroups(
        [Description("The search pattern to match groups (supports partial matches).")] string searchPattern,
        [Description("Sort direction: asc or desc (default: asc).")] string sort = "asc",
        [Description("Order by field: id, name, path, or created_at (default: name).")] string orderBy = "name",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GroupOperations.SearchGroups(searchPattern, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, orderBy, sort);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_group"), Description("Creates a new GitLab group or subgroup.")]
    public async Task<string> CreateGroup(
        [Description("The name of the group to create.")] string name,
        [Description("Optional parent group ID to create a subgroup.")] int? parentId = null,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GroupOperations.CreateGitLabGroup(name, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, parentId);
        return ToolHelper.Serialize(result);
    }
}
