using System.ComponentModel;
using Garrard.GitLab;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="GroupVariablesOperations"/>.</summary>
[McpServerToolType]
public sealed class GroupVariableTools(IOptions<GitLabOptions> options)
{
    private readonly GitLabOptions _opts = options.Value;

    [McpServerTool(Name = "gitlab_get_group_variable"), Description("Gets a specific variable from a GitLab group.")]
    public async Task<string> GetGroupVariable(
        [Description("The ID of the group.")] string groupId,
        [Description("The key of the variable to retrieve.")] string variableKey,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GroupVariablesOperations.GetGroupVariable(groupId, variableKey, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_or_update_group_variable"), Description("Creates or updates a variable in a GitLab group.")]
    public async Task<string> CreateOrUpdateGroupVariable(
        [Description("The ID of the group.")] string groupId,
        [Description("The variable key.")] string variableKey,
        [Description("The variable value.")] string variableValue,
        [Description("Variable type: env_var or file (default: env_var).")] string variableType = "env_var",
        [Description("Whether the variable is protected (default: false).")] bool isProtected = false,
        [Description("Whether the variable is masked (default: false).")] bool isMasked = false,
        [Description("Environment scope (default: *).")] string? environmentScope = "*",
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GroupVariablesOperations.CreateOrUpdateGroupVariable(
            groupId, variableKey, variableValue, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain,
            variableType, isProtected, isMasked, environmentScope);
        return ToolHelper.Serialize(result);
    }
}
