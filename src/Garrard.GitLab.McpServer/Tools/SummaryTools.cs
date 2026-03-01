using System.ComponentModel;
using Garrard.GitLab;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="SummaryOperations"/>.</summary>
[McpServerToolType]
public sealed class SummaryTools(IOptions<GitLabOptions> options)
{
    private readonly GitLabOptions _opts = options.Value;

    [McpServerTool(Name = "gitlab_get_group_summary"), Description("Gets a summary of a GitLab group including subgroup and project counts.")]
    public async Task<string> GetGroupSummary(
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await SummaryOperations.GetGroupSummary(groupIdOrName, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_summary"), Description("Gets a summary of a GitLab project including variable count and basic information.")]
    public async Task<string> GetProjectSummary(
        [Description("The numeric ID of the project.")] int projectId,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await SummaryOperations.GetProjectSummary(projectId, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_group_projects_summary"), Description("Gets summaries of all projects in a GitLab group.")]
    public async Task<string> GetGroupProjectsSummary(
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Whether to include projects from subgroups (default: true).")] bool includeSubgroups = true,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await SummaryOperations.GetGroupProjectsSummary(groupIdOrName, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain, includeSubgroups);
        return ToolHelper.Serialize(result);
    }
}
