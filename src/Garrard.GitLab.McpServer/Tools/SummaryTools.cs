using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="Garrard.GitLab.SummaryOperations"/>.</summary>
[McpServerToolType]
public static class SummaryTools
{
    [McpServerTool(Name = "gitlab_get_group_summary"), Description("Gets a summary of a GitLab group including subgroup and project counts.")]
    public static async Task<string> GetGroupSummary(
        IConfiguration config,
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await SummaryOperations.GetGroupSummary(groupIdOrName, resolvedPat, resolvedDomain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_summary"), Description("Gets a summary of a GitLab project including variable count and basic information.")]
    public static async Task<string> GetProjectSummary(
        IConfiguration config,
        [Description("The numeric ID of the project.")] int projectId,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await SummaryOperations.GetProjectSummary(projectId, resolvedPat, resolvedDomain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_group_projects_summary"), Description("Gets summaries of all projects in a GitLab group.")]
    public static async Task<string> GetGroupProjectsSummary(
        IConfiguration config,
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Whether to include projects from subgroups (default: true).")] bool includeSubgroups = true,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await SummaryOperations.GetGroupProjectsSummary(groupIdOrName, resolvedPat, resolvedDomain, includeSubgroups);
        return ToolHelper.Serialize(result);
    }
}
