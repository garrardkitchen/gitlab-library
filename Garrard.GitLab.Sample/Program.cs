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
        
        
        var subgroups = await GroupOperations.GetSubgroups(
            "1437",     // Group ID or name
            gitlabPat,           // Personal Access Token
            gitlabDomain,        // GitLab domain
            "name",              // Order by field (optional, default: name)
            "asc",               // Sort direction (optional, default: asc)
            Console.WriteLine    // Optional message handler
        );
        
        if (subgroups.IsSuccess)
        {
            Console.WriteLine($"Found {subgroups.Value.Length} subgroups");
            
            foreach (var group in subgroups.Value)
            {
                Console.WriteLine($"Group: {group.Name} (ID: {group.Id})");
                Console.WriteLine($"  Path: {group.FullPath}");
                Console.WriteLine($"  URL: {group.WebUrl}");
                Console.WriteLine($"  Has subgroups: {group.HasSubgroups}");
            }
        }
        
        // Get all projects in a group
        var projects = await ProjectOperations.GetProjectsInGroup(
            "1437",     // Group ID or name
            gitlabPat,           // Personal Access Token
            gitlabDomain,        // GitLab domain
            true,                // Include subgroups (optional, default: true)
            "name",              // Order by field (optional, default: name)
            "asc",               // Sort direction (optional, default: asc)
            Console.WriteLine    // Optional message handler
        );
        
        if (projects.IsSuccess)
        {
            Console.WriteLine($"Found {projects.Value.Length} projects");
            
            foreach (var project in projects.Value)
            {
                Console.WriteLine($"Project: {project.Name} (ID: {project.Id})");
                Console.WriteLine($"  Group ID: {project.GroupId}");
                Console.WriteLine($"  Path: {project.Path}");
                Console.WriteLine($"  Namespace: {project.Namespace.FullPath}");
                Console.WriteLine($"  URL: {project.WebUrl}");
                Console.WriteLine($"  Last activity: {project.LastActivityAt}");
            }
        }

        return;
        
        // Search and replace example:
        
        FileOperations.CreateFileWithContent($"./", ".gitlab-ci.yml", $"TF_VAR_TFE_WORKSPACE_NAME: \"<enter-workload-name>\"");

        var replacePlaceholderInFile = await FileOperations.ReplacePlaceholderInFile("./.gitlab-ci.yml", "TF_VAR_TFE_WORKSPACE_NAME", "\"<enter-workload-name>\"", "\"foo\"", ":", Console.WriteLine);

        if (replacePlaceholderInFile.IsFailure) 
        {
            Console.WriteLine(replacePlaceholderInFile.Error);
        }
        
        // Create or update a group variable
        var result = await GroupVariablesOperations.CreateOrUpdateGroupVariable(
            "1607",              // Group ID
            "NEW_VAR",           // Variable key
            "FOO",               // Variable value
            gitlabPat,           // Personal Access Token
            gitlabDomain,        // GitLab domain
            "env_var",           // Variable type (optional, default: env_var)
            false,               // Is protected (optional, default: false)
            true,                // Is masked (optional, default: false) - HTTP API fails if used so not included
            "*",                 // Environment scope (optional, default: *)
             Console.WriteLine
        );

        if (result.IsSuccess)
        {
            Console.WriteLine($"Variable created/updated successfully");
        }

        var variable = await GroupVariablesOperations.GetGroupVariable(
            "1607",              // Group ID
            "NEW_VAR",           // Variable key
            gitlabPat,           // Personal Access Token
            gitlabDomain         // GitLab domain
        );

        if (variable.IsSuccess)
        {
            Console.WriteLine($"Variable value: {variable.Value.Value}");
        }

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
        
        newProjectName = projectCreation.Value.Name;

        AnsiConsole.MarkupLine($"[green][bold]{newProjectName}({projectCreation.Value.Id})[/] has been created![/]");
        
        AnsiConsole.MarkupLine($"[yellow]Downloading [orangered1]{repoUrl}[/][/]");
        
        // Download a git repository from GitLab
        
        GitOperations.DownloadGitRepository(repoUrl, clonePath, pat: gitlabPat);

        AnsiConsole.MarkupLine($"[yellow]Cloning [orangered1]{projectCreation.Value.HttpUrlToRepo}[/][/]");
        
        // Clone the repository

        GitOperations.CloneGitLabProject($"{projectCreation.Value.HttpUrlToRepo}", $"{rootFolder}/{newProjectName}", pat: gitlabPat);
        
        // Create a README.md then push to main

        FileOperations.CreateFileWithContent($"{rootFolder}/{newProjectName}", "README.md", $"# {projectCreation.Value.Name}");

        GitOperations.BranchCommitPushChanges($"{rootFolder}/{newProjectName}", "initial commit", "main");

        // Copy files from the downloaded repository to the new project

        AnsiConsole.MarkupLine($"[yellow]Copying files from [orangered1]{clonePath}[/] into [orangered1]./{newProjectName}[/][/]");

        FileOperations.CopyFiles(clonePath, $"{rootFolder}/{newProjectName}");

        AnsiConsole.MarkupLine($"[yellow]Commit changes and pushing to [orangered1]gitlab:{newProjectName}[/][/]");

        // Perform a git commit and push
        
        GitOperations.BranchCommitPushChanges($"{rootFolder}/{newProjectName}", commitMessage, branchName);
        
        AnsiConsole.MarkupLine($"[green]Tidying up by removing the [orangered1]{clonePath}[/] folder[/]");
        
        // Remove the temporary folder
        
        FileOperations.RemoveTempFolder(clonePath);
        
        // Move project to another group

        var moveProjectToGroup = await GitOperations.TransferProjectToGroupOrNamespace(projectCreation.Value.Id, 1607, gitlabPat, gitlabDomain);

        if (moveProjectToGroup.IsFailure)
        {
            AnsiConsole.MarkupLine($"[red]{moveProjectToGroup.Error}[/]");
        }
        
        AnsiConsole.MarkupLine($"[green][bold]Workflow completed successfully![/][/]");
    }
}
