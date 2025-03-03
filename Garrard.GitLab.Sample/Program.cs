using Garrard.GitLab;
using Spectre.Console;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // Obtain values from Secrets, then environment variables
        // I have coded this here purely for this specific DX. When you use this library, it will be down to you to obtain these default values 
        
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var gitlabPat = configuration["GL_PAT"] ?? throw new ArgumentNullException("GL_PAT", "GitLab PAT is not set in user secrets or environment variables.");
        var gitlabDomain = configuration["GL_DOMAIN"] ?? throw new ArgumentNullException("GL_DOMAIN", "GitLab domain is not set in user secrets or environment variables.");
        var gitlabNamespace = configuration["GL_NAMESPACE"] ?? throw new ArgumentNullException("GL_NAMESPACE", "GitLab namespace is not set in user secrets or environment variables.");
            
        // prompt the user for input
        
        AnsiConsole.MarkupLine($"[yellow][bold]Prompt for values:[/][/]");
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var rootFolder = AnsiConsole.Ask<string>(" - Enter the root folder for the project:",$"{homeDirectory}/source/ai");
        var repoUrl = AnsiConsole.Ask<string>(" - Enter the GitLab repository URL:","https://github.com/garrardkitchen/fujitsu-pro-aspire-demo.git");
        var commitMessage = AnsiConsole.Ask<string>(" - Enter the commit message:", "initial commit");
        var newProjectName = AnsiConsole.Ask<string>(" - Enter the name for the new GitLab project:", "gpk-deleteme");
        var clonePath = AnsiConsole.Ask<string>(" - Enter the local path to clone the repository:",$"{rootFolder}/{newProjectName}-tmp");
        var branchName = AnsiConsole.Ask<string>(" - Enter a branch name (if empty, it'll use your mainline branch)", "");

        // summary statement, with option to back out
        
        AnsiConsole.MarkupLine($"[yellow][bold]Summary of actions:[/][/]");
        AnsiConsole.MarkupLine($" - [yellow]Working folder is [orangered1]{rootFolder}[/][/]");
        AnsiConsole.MarkupLine($" - [yellow]Create a new GitLab Project called [orangered1]{newProjectName}[/][/]");
        AnsiConsole.MarkupLine($" - [yellow]Clone [orangered1]{repoUrl}[/] to folder [greenyellow]{clonePath}[/][/]");
        AnsiConsole.MarkupLine($" - [yellow]Copy files (except .git/) from [greenyellow]{clonePath}[/] to [orangered1]{rootFolder}/{newProjectName}[/][/]");
        AnsiConsole.MarkupLine(" - [yellow]Branch [orangered1]{0}[/][/]", string.IsNullOrEmpty(branchName) ? "(mainline)" : branchName);
        AnsiConsole.MarkupLine($" - [yellow]Commit and push cloned repo to [orangered1]{newProjectName}[/][/]");
        
        var confirmToContinue = AnsiConsole.Prompt(
            new TextPrompt<bool>("Do you want to continue?")
                .AddChoice(true)
                .AddChoice(false)
                .DefaultValue(false)
                .WithConverter(choice => choice ? "y" : "n"));

        if (!confirmToContinue) return;
        
        // start the workflow of creating a new GitLab project, cloning the repository, copying files, and committing changes
        
        AnsiConsole.MarkupLine($"[yellow]Creating a new GitLab project named [orangered1]{newProjectName}[/][/]");
        
        // Create a new GitLab project
        
        var projectCreation = await GitOperations.CreateGitLabProject(newProjectName, gitlabPat, gitlabDomain,
            projectName =>
            {
                AnsiConsole.MarkupLine($"[yellow] - [orangered1]{projectName}[/] exists, establishing an available project name...[/]");
            });
        if (projectCreation.IsFailure)
        {
            AnsiConsole.MarkupLine($"[red]{projectCreation.Error}. Exiting...[/]");
            return;
        }

        // will use the new name
        newProjectName = projectCreation.Value;

        AnsiConsole.MarkupLine($"[green][bold]{newProjectName}[/] has been created![/]");
        
        AnsiConsole.MarkupLine($"[yellow]Downloading [orangered1]{repoUrl}[/][/]");
        
        // Download a git repository from GitLab
        
        GitOperations.DownloadGitRepository(repoUrl, clonePath, pat: gitlabPat);

        AnsiConsole.MarkupLine($"[yellow]Cloning [orangered1]{repoUrl}[/][/]");
        
        // Clone the repository

        GitOperations.CloneGitLabProject($"https://{gitlabDomain}/{gitlabNamespace}/{newProjectName}.git", $"{rootFolder}/{newProjectName}", pat: gitlabPat);
        
        AnsiConsole.MarkupLine($"[yellow]Copying files from [orangered1]{clonePath}[/] into [orangered1]./{newProjectName}[/][/]");

        // Copy files from the downloaded repository to the new project
        
        FileOperations.CopyFiles(clonePath, $"{rootFolder}/{newProjectName}");

        AnsiConsole.MarkupLine($"[yellow]Commit changes and pushing to [orangered1]gitlab:{newProjectName}[/][/]");

        // Perform a git commit and push
        
        GitOperations.BranchCommitPushChanges($"{rootFolder}/{newProjectName}", commitMessage, branchName);
        
        AnsiConsole.MarkupLine($"[green]Tidying up by removing the [orangered1]{clonePath}[/] folder[/]");
        
        // Remove the temporary folder
        
        FileOperations.RemoveTempFolder(clonePath);
        
        AnsiConsole.MarkupLine($"[green][bold]Workflow completed successfully![/][/]");
    }
}
