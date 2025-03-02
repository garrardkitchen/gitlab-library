using System.Diagnostics;
using System.Net.Http.Headers;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab;

public class GitOperations
{
    public static async Task<Result<string>> CreateGitLabProject(string projectName, string pat, string gitlabDomain)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);

        var content = new StringContent($"{{ \"name\": \"{projectName}\" }}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://{gitlabDomain}/api/v4/projects", content);

        if (response.IsSuccessStatusCode)
        {
            return Result.Success("Project created successfully!");
        }
        else
        {
            await response.Content.ReadAsStringAsync();
            return Result.Failure<string>("Failed to create project");
        }
    }

    public static void DownloadGitRepository(string repoUrl, string clonePath)
    {
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

    public static void CommitAndPushChanges(string repoPath, string commitMessage)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C {repoPath} add .",
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

        startInfo.Arguments = $"-C {repoPath} commit -m \"{commitMessage}\"";
        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }

        startInfo.Arguments = $"-C {repoPath} push";
        using (Process process = Process.Start(startInfo)!)
        {
            process.WaitForExit();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());
        }
    }

    public static void CloneGitLabProject(string repoUrl, string clonePath)
    {
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
}

