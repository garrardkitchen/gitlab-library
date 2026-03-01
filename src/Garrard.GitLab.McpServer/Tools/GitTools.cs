using System.ComponentModel;
using Garrard.GitLab;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>MCP tools that wrap <see cref="GitOperations"/>.</summary>
[McpServerToolType]
public sealed class GitTools(IOptions<GitLabOptions> options)
{
    private readonly GitLabOptions _opts = options.Value;

    [McpServerTool(Name = "gitlab_create_project_legacy"), Description("Creates a new GitLab project, automatically handling name conflicts by appending a numeric suffix.")]
    public async Task<string> CreateGitLabProject(
        [Description("The desired name for the new project.")] string projectName,
        [Description("Optional group ID to create the project under.")] string? groupId = null,
        [Description("Whether to enable shared runners (default: false).")] bool sharedRunnersEnabled = false,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GitOperations.CreateGitLabProject(
            projectName, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain,
            _ => { },
            groupId,
            sharedRunnersEnabled);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_transfer_project"), Description("Transfers a GitLab project to a different group or namespace.")]
    public async Task<string> TransferProjectToGroupOrNamespace(
        [Description("The ID of the project to transfer.")] string projectId,
        [Description("The ID of the destination group or namespace.")] short newGroupId,
        [Description("Override GitLab domain. Falls back to configured default.")] string? gitlabDomain = null,
        [Description("Override GitLab PAT. Falls back to configured default.")] string? pat = null)
    {
        var result = await GitOperations.TransferProjectToGroupOrNamespace(projectId, newGroupId, pat ?? _opts.Pat, gitlabDomain ?? _opts.Domain);
        return ToolHelper.Serialize(result);
    }

    [McpServerTool(Name = "gitlab_download_repository"), Description("Downloads (clones) a Git repository to a local path. Optionally authenticates with a PAT and checks out a specific branch.")]
    public string DownloadGitRepository(
        [Description("The URL of the repository to download.")] string repoUrl,
        [Description("The local path to clone the repository to.")] string clonePath,
        [Description("Optional branch name to check out.")] string? branchName = null,
        [Description("Override GitLab PAT for authenticated clones. Falls back to configured default.")] string? pat = null)
    {
        GitOperations.DownloadGitRepository(repoUrl, clonePath, branchName, pat ?? _opts.Pat);
        return $"Repository downloaded to {clonePath}";
    }

    [McpServerTool(Name = "gitlab_clone_project"), Description("Clones a GitLab project repository to a local path.")]
    public string CloneGitLabProject(
        [Description("The HTTP URL of the GitLab repository.")] string repoUrl,
        [Description("The local path to clone the repository to.")] string clonePath,
        [Description("Override GitLab PAT for authentication. Falls back to configured default.")] string? pat = null)
    {
        GitOperations.CloneGitLabProject(repoUrl, clonePath, pat ?? _opts.Pat);
        return $"Repository cloned to {clonePath}";
    }

    [McpServerTool(Name = "gitlab_create_branch"), Description("Creates a new branch in a local Git repository.")]
    public string CreateBranch(
        [Description("The local path of the repository.")] string repoPath,
        [Description("The name of the branch to create.")] string branchName)
    {
        GitOperations.CreateBranch(repoPath, branchName);
        return $"Branch '{branchName}' created in {repoPath}";
    }

    [McpServerTool(Name = "gitlab_push_changes"), Description("Pushes committed changes in a local repository to the remote.")]
    public string PushChanges(
        [Description("The local path of the repository.")] string repoPath,
        [Description("Optional branch name to push to (pushes current branch if omitted).")] string? branchName = null)
    {
        GitOperations.PushChanges(repoPath, branchName);
        return $"Changes pushed from {repoPath}";
    }

    [McpServerTool(Name = "gitlab_branch_commit_push"), Description("Creates a branch (optional), stages all changes, commits, and pushes to a local Git repository.")]
    public string BranchCommitPushChanges(
        [Description("The local path of the repository.")] string repoPath,
        [Description("The commit message.")] string commitMessage,
        [Description("Optional branch name to create and push to.")] string? branchName = null)
    {
        GitOperations.BranchCommitPushChanges(repoPath, commitMessage, branchName);
        return $"Changes committed and pushed from {repoPath}";
    }
}
