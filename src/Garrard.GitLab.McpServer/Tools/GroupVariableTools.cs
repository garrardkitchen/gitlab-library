using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="Garrard.GitLab.GroupVariablesOperations"/>.</summary>
[McpServerToolType]
public static class GroupVariableTools
{
    [McpServerTool(Name = "gitlab_get_group_variable"), Description("Gets a specific variable from a GitLab group.")]
    public static async Task<string> GetGroupVariable(
        IConfiguration config,
        [Description("The ID of the group.")] string groupId,
        [Description("The key of the variable to retrieve.")] string variableKey,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GroupVariablesOperations.GetGroupVariable(groupId, variableKey, resolvedPat, resolvedDomain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_create_or_update_group_variable"), Description("Creates or updates a variable in a GitLab group.")]
    public static async Task<string> CreateOrUpdateGroupVariable(
        IConfiguration config,
        [Description("The ID of the group.")] string groupId,
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
        var result = await GroupVariablesOperations.CreateOrUpdateGroupVariable(
            groupId, variableKey, variableValue, resolvedPat, resolvedDomain,
            variableType, isProtected, isMasked, environmentScope);
        return ToolHelper.Serialize(result);
    }
}
