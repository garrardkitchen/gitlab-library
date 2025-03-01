using System.Diagnostics;
using Spectre.Console;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitToolLibrary;


public class GitToolApi
{
    public static async Task<bool> CreateGitLabProject(string projectName, string pat, string gitlabDomain)
    {
        var gitLabToken = AnsiConsole.Ask<string>("Enter your GitLab private token:", pat);
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gitLabToken);

        var content = new StringContent($"{{ \"name\": \"{projectName}\" }}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://{gitlabDomain}/api/v4/projects", content);

        if (response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine("[green]Project created successfully![/]");
            return true;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Failed to create project.[/]");
            AnsiConsole.WriteLine(await response.Content.ReadAsStringAsync());
            return false;
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

    public static void CopyFiles(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            if (dirPath.EndsWith(".git") || dirPath.Contains(".git/"))
            {
                Console.WriteLine(dirPath);
                continue;
            }

            Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
        }

        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            if (newPath.EndsWith(".git") || newPath.Contains(".git/"))
            {
                Console.WriteLine(newPath);
                continue;
            }
            File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
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

