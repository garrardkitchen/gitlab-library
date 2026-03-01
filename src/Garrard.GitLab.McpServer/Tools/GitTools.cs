using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="Garrard.GitLab.GitOperations"/>.</summary>
[McpServerToolType]
public static class GitTools
{
    [McpServerTool(Name = "gitlab_create_project_legacy"), Description("Creates a new GitLab project, automatically handling name conflicts by appending a numeric suffix.")]
    public static async Task<string> CreateGitLabProject(
        IConfiguration config,
        [Description("The desired name for the new project.")] string projectName,
        [Description("Optional group ID to create the project under.")] string? groupId = null,
        [Description("Whether to enable shared runners (default: false).")] bool sharedRunnersEnabled = false,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GitOperations.CreateGitLabProject(
            projectName, resolvedPat, resolvedDomain,
            _ => { }, // no-op for onProjectExists in MCP context
            groupId,
            sharedRunnersEnabled);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_transfer_project"), Description("Transfers a GitLab project to a different group or namespace.")]
    public static async Task<string> TransferProjectToGroupOrNamespace(
        IConfiguration config,
        [Description("The ID of the project to transfer.")] string projectId,
        [Description("The ID of the destination group or namespace.")] short newGroupId,
        [Description("GitLab domain (e.g. gitlab.com). Defaults to GL_DOMAIN env var.")] string? gitlabDomain = null,
        [Description("GitLab Personal Access Token. Defaults to GL_PAT env var.")] string? pat = null)
    {
        var resolvedPat = ToolHelper.Resolve(config, pat, "GL_PAT", "pat");
        var resolvedDomain = ToolHelper.Resolve(config, gitlabDomain, "GL_DOMAIN", "gitlabDomain");
        var result = await GitOperations.TransferProjectToGroupOrNamespace(projectId, newGroupId, resolvedPat, resolvedDomain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_download_repository"), Description("Downloads (clones) a Git repository to a local path. Optionally authenticates with a PAT and checks out a specific branch.")]
    public static string DownloadGitRepository(
        [Description("The URL of the repository to download.")] string repoUrl,
        [Description("The local path to clone the repository to.")] string clonePath,
        [Description("Optional branch name to check out.")] string? branchName = null,
        [Description("Optional GitLab Personal Access Token for authenticated clones.")] string? pat = null)
    {
        GitOperations.DownloadGitRepository(repoUrl, clonePath, branchName, pat);
        return $"Repository downloaded to {clonePath}";
    }

    [McpServerTool(Name = "gitlab_clone_project"), Description("Clones a GitLab project repository to a local path.")]
    public static string CloneGitLabProject(
        [Description("The HTTP URL of the GitLab repository.")] string repoUrl,
        [Description("The local path to clone the repository to.")] string clonePath,
        [Description("Optional GitLab Personal Access Token for authentication.")] string? pat = null)
    {
        GitOperations.CloneGitLabProject(repoUrl, clonePath, pat);
        return $"Repository cloned to {clonePath}";
    }

    [McpServerTool(Name = "gitlab_create_branch"), Description("Creates a new branch in a local Git repository.")]
    public static string CreateBranch(
        [Description("The local path of the repository.")] string repoPath,
        [Description("The name of the branch to create.")] string branchName)
    {
        GitOperations.CreateBranch(repoPath, branchName);
        return $"Branch '{branchName}' created in {repoPath}";
    }

    [McpServerTool(Name = "gitlab_push_changes"), Description("Pushes committed changes in a local repository to the remote.")]
    public static string PushChanges(
        [Description("The local path of the repository.")] string repoPath,
        [Description("Optional branch name to push to (pushes current branch if omitted).")] string? branchName = null)
    {
        GitOperations.PushChanges(repoPath, branchName);
        return $"Changes pushed from {repoPath}";
    }

    [McpServerTool(Name = "gitlab_branch_commit_push"), Description("Creates a branch (optional), stages all changes, commits, and pushes to a local Git repository.")]
    public static string BranchCommitPushChanges(
        [Description("The local path of the repository.")] string repoPath,
        [Description("The commit message.")] string commitMessage,
        [Description("Optional branch name to create and push to.")] string? branchName = null)
    {
        GitOperations.BranchCommitPushChanges(repoPath, commitMessage, branchName);
        return $"Changes committed and pushed from {repoPath}";
    }
}
