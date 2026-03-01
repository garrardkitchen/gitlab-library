using System.Diagnostics;
using System.Net;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Garrard.GitLab.Library.DTOs;
using Garrard.GitLab.Library.Http;
using Garrard.GitLab.Library.Requests;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Library;

/// <summary>
/// Instance client for Git and GitLab project operations.
/// HTTP methods use <see cref="IGitLabHttpClientFactory"/>; process methods embed the PAT directly in URLs.
/// </summary>
public sealed class GitClient
{
    private readonly IGitLabHttpClientFactory _factory;
    private readonly GitLabOptions _opts;

    public GitClient(IGitLabHttpClientFactory factory, IOptions<GitLabOptions> options)
    {
        _factory = factory;
        _opts = options.Value;
    }

    /// <summary>
    /// Creates a new GitLab project, automatically handling name conflicts by appending a numeric suffix.
    /// </summary>
    public async Task<Result<(string Id, string Name, string HttpUrlToRepo, string PathWithNamespace)>> CreateGitLabProject(
        string projectName,
        Action<string> onProjectExists,
        string? groupId = null,
        bool sharedRunnersEnabled = false)
    {
        var client = _factory.CreateClient();

        int suffix = 0;
        string newProjectName = projectName;
        while (true)
        {
            var checkResponse = await client.GetAsync($"https://{_opts.Domain}/api/v4/projects?search={newProjectName}");
            if (checkResponse.IsSuccessStatusCode)
            {
                var found = await checkResponse.Content.ReadAsStringAsync();
                var gitLabProject = JsonSerializer.Deserialize<List<GitLabProjectReference>>(found);

                if (gitLabProject != null && gitLabProject.Any(x => x.Name.Equals(newProjectName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    onProjectExists(newProjectName);
                    suffix++;
                    newProjectName = $"{projectName}-{suffix}";
                    continue;
                }

                break;
            }

            if (checkResponse.StatusCode == HttpStatusCode.Unauthorized)
                return Result.Failure<(string, string, string, string)>($"{checkResponse.StatusCode}. Please check your Personal Access Token.");

            return Result.Failure<(string, string, string, string)>($"{checkResponse.StatusCode}");
        }

        var requestPayload = new CreateProjectRequest
        {
            Name = newProjectName,
            NamespaceId = groupId,
            SharedRunnersEnabled = sharedRunnersEnabled
        };
        var jsonPayload = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://{_opts.Domain}/api/v4/projects", content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseBody);
            var projectDetails = (
                Id: jsonResponse.RootElement.GetProperty("id").GetInt32().ToString(),
                Name: newProjectName,
                HttpUrlToRepo: jsonResponse.RootElement.GetProperty("http_url_to_repo").GetString() ?? string.Empty,
                PathWithNamespace: jsonResponse.RootElement.GetProperty("path_with_namespace").GetString() ?? string.Empty
            );
            return Result.Success(projectDetails);
        }
        else
        {
            await response.Content.ReadAsStringAsync();
            return Result.Failure<(string, string, string, string)>("Failed to create project");
        }
    }

    /// <summary>
    /// Transfers a GitLab project to a different group or namespace.
    /// </summary>
    public async Task<Result> TransferProjectToGroupOrNamespace(string projectId, int newGroupId)
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsync(
            $"https://{_opts.Domain}/api/v4/projects/{projectId}/transfer?namespace={newGroupId}", null);

        if (response.IsSuccessStatusCode)
            return Result.Success();

        var errorResponse = await response.Content.ReadAsStringAsync();
        return Result.Failure($"Failed to move project: {errorResponse}");
    }

    /// <summary>
    /// Downloads (clones) a Git repository to a local path. Embeds the PAT for authenticated clones.
    /// </summary>
    public void DownloadGitRepository(string repoUrl, string clonePath, string? branchName = null)
    {
        var pat = _opts.Pat;
        if (!string.IsNullOrEmpty(pat))
        {
            var uri = new Uri(repoUrl);
            repoUrl = $"{uri.Scheme}://oauth2:{pat}@{uri.Host}{uri.PathAndQuery}";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = branchName == null
                ? $"clone {repoUrl} {clonePath}"
                : $"clone -b {branchName} {repoUrl} {clonePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
        Console.WriteLine(process.StandardOutput.ReadToEnd());
        Console.WriteLine(process.StandardError.ReadToEnd());
    }

    /// <summary>Creates a new branch in a local Git repository.</summary>
    public void CreateBranch(string repoPath, string branchName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C {repoPath} checkout -b {branchName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
        Console.WriteLine(process.StandardOutput.ReadToEnd());
        Console.WriteLine(process.StandardError.ReadToEnd());
    }

    /// <summary>Pushes committed changes in a local repository to the remote.</summary>
    public void PushChanges(string repoPath, string? branchName = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = string.IsNullOrEmpty(branchName)
                ? $"-C {repoPath} push"
                : $"-C {repoPath} push -u origin {branchName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
        Console.WriteLine(process.StandardOutput.ReadToEnd());
        Console.WriteLine(process.StandardError.ReadToEnd());
    }

    /// <summary>Creates a branch (optional), stages all changes, commits, and pushes.</summary>
    public void BranchCommitPushChanges(string repoPath, string commitMessage, string? branchName = null)
    {
        if (!string.IsNullOrEmpty(branchName))
            CreateBranch(repoPath, branchName);

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Arguments = $"-C {repoPath} add .";
        using (var process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }

        startInfo.Arguments = $"-C {repoPath} commit -m \"{commitMessage}\"";
        using (var process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }

        PushChanges(repoPath, branchName);
    }

    /// <summary>Clones a GitLab project repository. Embeds the PAT for authentication.</summary>
    public void CloneGitLabProject(string repoUrl, string clonePath)
    {
        var pat = _opts.Pat;
        if (!string.IsNullOrEmpty(pat))
        {
            var uri = new Uri(repoUrl);
            repoUrl = $"{uri.Scheme}://oauth2:{pat}@{uri.Host}{uri.PathAndQuery}";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone {repoUrl} {clonePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
        Console.WriteLine(process.StandardOutput.ReadToEnd());
        Console.WriteLine(process.StandardError.ReadToEnd());
    }
}
