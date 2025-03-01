using System.Diagnostics;
using Spectre.Console;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {

        var gitlabPAT = Environment.GetEnvironmentVariable("GL_PAT");
        var gitlabDomain = Environment.GetEnvironmentVariable("GL_DOMAIN");
        var gitlabNamespace = Environment.GetEnvironmentVariable("GL_NAMESPACE");
        var repoUrl = AnsiConsole.Ask<string>("Enter the GitLab repository URL:","https://github.com/garrardkitchen/fujitsu-pro-aspire-demo.git");
        var clonePath = AnsiConsole.Ask<string>("Enter the local path to clone the repository:","/Users/garrardkitchen/source/ai/tmp");
        var commitMessage = AnsiConsole.Ask<string>("Enter the commit message:", "initial commit");
        var newProjectName = AnsiConsole.Ask<string>("Enter the name for the new GitLab project:", "gpk-deleteme");

        AnsiConsole.MarkupLine($"[bold]Creating a new GitLab project named {newProjectName}[/]");
        
        // Create a new GitLab project
        var projectHasBeenCreated = await CreateGitLabProject(newProjectName, gitlabPAT, gitlabDomain);
        if (!projectHasBeenCreated)
        {
            return;
        }
        
        AnsiConsole.MarkupLine($"[bold]Downloading {repoUrl}[/]");
        // Download a git repository from GitLab
        DownloadGitRepository(repoUrl, clonePath);

        AnsiConsole.MarkupLine($"[bold]Cloning {repoUrl}[/]");

        CloneGitLabProject($"https://{gitlabDomain}/{gitlabNamespace}/{newProjectName}.git", $"/Users/garrardkitchen/source/ai/{newProjectName}");
        
        // Copy files from the downloaded repository to the new project
        CopyFiles(clonePath, $"/Users/garrardkitchen/source/ai/{newProjectName}");

        // Perform a git commit and push
        CommitAndPushChanges($"/Users/garrardkitchen/source/ai/{newProjectName}", commitMessage);
    }

    static async Task<bool> CreateGitLabProject(string projectName, string pat, string gitlabDomain)
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

    static void DownloadGitRepository(string repoUrl, string clonePath)
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

    static void CopyFiles(string sourcePath, string destinationPath)
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

    static void CommitAndPushChanges(string repoPath, string commitMessage)
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

    static void CloneGitLabProject(string repoUrl, string clonePath)
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
