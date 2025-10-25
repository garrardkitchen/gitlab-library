using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab;

public class GitLabProjectDto
{
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("http_url_to_repo")]
    public string HttpUrlToRepo { get; set; }
    
    [JsonPropertyName("path_with_namespace")]
    public string PathWithNamespace { get; set; }
}

public class GitOperations
{
    public static async Task<Result<(string Id, string Name, string HttpUrlToRepo, string PathWithNamespace)>> CreateGitLabProject(string projectName, string pat, string gitlabDomain, Action<string> onProjectExists, string? groupId = null, bool sharedRunnersEnabled = false)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);

        int suffix = 0;
        string newProjectName = projectName;
        while (true)
        {
            var checkResponse = await client.GetAsync($"https://{gitlabDomain}/api/v4/projects?search={newProjectName}");
            if (checkResponse.IsSuccessStatusCode)
            {
                var found = await checkResponse.Content.ReadAsStringAsync();
             
                var gitLabProject = JsonSerializer.Deserialize<List<GitLabProjectDto>>(found);

                if (gitLabProject != null && gitLabProject.Any(x=>x.Name.Equals(newProjectName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    onProjectExists(newProjectName);
                    suffix++;
                    newProjectName = $"{projectName}-{suffix}";
                    continue;
                }

                break;
            }

            if (checkResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result.Failure<(string, string, string, string)>($"{checkResponse.StatusCode.ToString()}. Please check your Personal Access Token.");
            }
                
            return Result.Failure<(string, string, string, string)>($"{checkResponse.StatusCode.ToString()}");
        }

        var content = new StringContent(groupId == null ? $"{{ \"name\": \"{newProjectName}\", \"shared_runners_enabled\": {sharedRunnersEnabled.ToString().ToLower()} }}" : $"{{ \"name\": \"{newProjectName}\", \"namespace_id\": \"{groupId}\", \"shared_runners_enabled\": {sharedRunnersEnabled.ToString().ToLower()} }}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://{gitlabDomain}/api/v4/projects", content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = System.Text.Json.JsonDocument.Parse(responseBody);
            var projectDetails = (
                Id: jsonResponse.RootElement.GetProperty("id").GetInt16().ToString(),
                Name: newProjectName,
                HttpUrlToRepo: jsonResponse.RootElement.GetProperty("http_url_to_repo").GetString(),
                PathWithNamespace: jsonResponse.RootElement.GetProperty("path_with_namespace").GetString()
            );
            return Result.Success(projectDetails);
        }
        else
        {
            await response.Content.ReadAsStringAsync();
            return Result.Failure<(string, string, string, string)>("Failed to create project");
        }
    }

    public static void DownloadGitRepository(string repoUrl, string clonePath, string? branchName = null, string? pat = null)
    {
        if (!string.IsNullOrEmpty(pat))
        {
            var uri = new Uri(repoUrl);
            repoUrl = $"{uri.Scheme}://oauth2:{pat}@{uri.Host}{uri.PathAndQuery}";
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = branchName == null ? $"clone {repoUrl} {clonePath}" : $"clone -b {branchName} {repoUrl} {clonePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }
    }

    public static void CreateBranch(string repoPath, string branchName)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C {repoPath} checkout -b {branchName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }
    }

    public static void PushChanges(string repoPath, string? branchName = null)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (string.IsNullOrEmpty(branchName))
        {
            startInfo.Arguments = $"-C {repoPath} push";
        }
        else
        {
            startInfo.Arguments = $"-C {repoPath} push -u origin {branchName}";
        }
        
        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }
    }

    public static void BranchCommitPushChanges(string repoPath, string commitMessage, string? branchName = null)
    {
        if (!string.IsNullOrEmpty(branchName))
        {
            CreateBranch(repoPath, branchName);
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Arguments = $"-C {repoPath} add .";
        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }

        startInfo.Arguments = $"-C {repoPath} commit -m \"{commitMessage}\"";
        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }

        PushChanges(repoPath, branchName);
    }

    public static void CloneGitLabProject(string repoUrl, string clonePath, string? pat = null)
    {
        
        if (!string.IsNullOrEmpty(pat))
        {
            var uri = new Uri(repoUrl);
            repoUrl = $"{uri.Scheme}://oauth2:{pat}@{uri.Host}{uri.PathAndQuery}";
        }
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone {repoUrl} {clonePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }
    }

    public static async Task<Result> TransferProjectToGroupOrNamespace(string projectId, Int16 newGroupId, string pat, string gitlabDomain)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);

        // var content = new StringContent($"{{ \"namespace\": \"{newGroupId}\" }}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"https://{gitlabDomain}/api/v4/projects/{projectId}/transfer?namespace={newGroupId}", null); //, content);

        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }
        else
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            return Result.Failure($"Failed to move project: {errorResponse}");
        }
    }
}

