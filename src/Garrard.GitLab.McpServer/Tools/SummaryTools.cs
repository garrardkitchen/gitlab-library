using System.ComponentModel;
using Garrard.GitLab.Library;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="SummaryClient"/>.</summary>
[McpServerToolType]
public sealed class SummaryTools(SummaryClient summaryClient)
{
    [McpServerTool(Name = "gitlab_get_group_summary"), Description("Gets a summary of a GitLab group including subgroup and project counts.")]
    public async Task<string> GetGroupSummary(
        [Description("The ID or name of the group.")] string groupIdOrName)
    {
        var result = await summaryClient.GetGroupSummary(groupIdOrName);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_project_summary"), Description("Gets a summary of a GitLab project including variable count and basic information.")]
    public async Task<string> GetProjectSummary(
        [Description("The numeric ID of the project.")] int projectId)
    {
        var result = await summaryClient.GetProjectSummary(projectId);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_get_group_projects_summary"), Description("Gets summaries of all projects in a GitLab group.")]
    public async Task<string> GetGroupProjectsSummary(
        [Description("The ID or name of the group.")] string groupIdOrName,
        [Description("Whether to include projects from subgroups (default: true).")] bool includeSubgroups = true)
    {
        var result = await summaryClient.GetGroupProjectsSummary(groupIdOrName, includeSubgroups);
        return ToolHelper.Serialize(result);
    }
}
