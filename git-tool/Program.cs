using GitToolLibrary;
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

        AnsiConsole.MarkupLine($"[yellow]Creating a new GitLab project named {newProjectName}[/]");
        
        // Create a new GitLab project
        var projectHasBeenCreated = await GitToolApi.CreateGitLabProject(newProjectName, gitlabPAT, gitlabDomain);
        if (!projectHasBeenCreated)
        {
            return;
        }
        
        AnsiConsole.MarkupLine($"[yellow]Downloading {repoUrl}[/]");
        // Download a git repository from GitLab
        GitToolApi.DownloadGitRepository(repoUrl, clonePath);

        AnsiConsole.MarkupLine($"[yellow]Cloning {repoUrl}[/]");

        GitToolApi.CloneGitLabProject($"https://{gitlabDomain}/{gitlabNamespace}/{newProjectName}.git", $"/Users/garrardkitchen/source/ai/{newProjectName}");
        
        AnsiConsole.MarkupLine($"[yellow]Copying files from {clonePath} into {newProjectName}[/]");

        // Copy files from the downloaded repository to the new project
        GitToolApi.CopyFiles(clonePath, $"/Users/garrardkitchen/source/ai/{newProjectName}");

        AnsiConsole.MarkupLine($"[yellow]Commit changes and pushing to gitlab:{newProjectName}[/]");

        // Perform a git commit and push
        GitToolApi.CommitAndPushChanges($"/Users/garrardkitchen/source/ai/{newProjectName}", commitMessage);
    }
}
