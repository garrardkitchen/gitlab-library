# Garrard.GitLab

Garrard.GitLab is a .NET library that provides operations for working with GitLab projects.

## Installation

To install `Garrard.GitLab`, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```powershell
Install-Package Garrard.GitLab -Version 0.0.15
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="0.0.15" />
```

Or use the dotnet add command:

```powershell
dotnet add package Garrard.GitLab --version 0.0.15
```

## Usage

Here is an example of how to use Garrard.GitLab in your project:

```csharp
using Garrard.GitLab;

class Program
{
    static async Task Main(string[] args)
    {
        // Example usage of GitLab.GitOperations, GitLab.FileOperations

        var projectCreation = await GitOperations.CreateGitLabProject("new-project-name", "your-gitlab-pat", "gitlab-domain", projectName =>
            {
                Console.WriteLine($" - {projectName} exists, establishing an available project name...");
            }, "group-id-or-omit-to-add-to-users-namespace");
        
        if (projectCreation.IsFailure)
        {
            Console.WriteLine($"{projectCreation.Error}. Exiting...");
            return;
        }
        
        // `projectCreation.Value.Name` will have changed if couldn't use the original name

        Console.WriteLine($"GitLab project `{projectCreation.Value.Name}` ({projectCreation.Value.HttpUrlToRepo}) created");
        
        GitOperations.DownloadGitRepository("https://github.com/yourusername/your-repo.git", "/path/to/download/to", "branch-name", "pat");
        GitOperations.CloneGitLabProject(projectCreation.Value.HttpUrlToRepo, "/path/to/clone", "pat");

        // Create a README.md then push to mainline branch

        FileOperations.CreateFileWithContent("/path/to/download/to", "README.md", $"# {projectCreation.Value.Name}");
        GitOperations.BranchCommitPushChanges("/path/to/clone", "initial commit", "main");

        // Copy files from the downloaded repository to the new project

        FileOperations.CopyFiles("/path/to/download/to", "/path/to/clone");
        GitOperations.BranchCommitPushChanges("/path/to/clone", "commit message", "branch-name-or-omit-to-use-mainline-branch");
        FileOperations.RemoveTempFolder("/path/to/download/to"); 
        
        // Move the project to a group
        
        var moveProjectToGroup = await GitOperations.TransferProjectToGroupOrNamespace(projectCreation.Value.Id, "group-id-or-namespace", "pat", "gitlab-domain");

        if (moveProjectToGroup.IsFailure)
        {
            AnsiConsole.MarkupLine($"[red]{moveProjectToGroup.Error}[/]");
        }

        // Create or update a Group variable
        
        var result = await GroupVariablesOperations.CreateOrUpdateGroupVariable(
            "1607",              // Group ID
            "NEW_VAR",           // Variable key
            "FOO",               // Variable value
            gitlabPat,           // Personal Access Token
            gitlabDomain,        // GitLab domain
            "env_var",           // Variable type (optional, default: env_var)
            false,               // Is protected (optional, default: false)
            true,                // Is masked (optional, default: false) - HTTP API fails if used so ignored for now
            "*",                 // Environment scope (optional, default: *)
             Console.WriteLine
        );

        if (result.IsSuccess)
        {
            Console.WriteLine($"Variable created/updated successfully");
        }

        // get a group variable
        
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
    }
}
```

## Features

- Create a new GitLab project 
  - It will create a unique project (by incrementing a number after your suggested name) if your suggested name exists
  - It will by default add the project to your user's namespace. If you supply a groupID, it will
    add the project to this group instead
  - Returns a tuple of (Id, Name, HttpUrlToRepo and PathWithNamespace)
  - Will return failure information if failed to access your GitLab account with your PAT
- Download an existing git repository 
  - You can provide branch name
  - You can provide a PAT (uses oauth2)
- Clone GitLab project
  - You can provide a PAT (uses oauth2)
- Copy files from cloned repo (or Project) to your new GitLab project folder (excluding the .git/ folder)
- Branch (optional), Commit and push changes
- Remove temporary folder
- Create a file with contents
- Transfer project to a different group (or namespace)
- Get a Group variable
- Create or update a Group variable

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/garrardkitchen/gitlab-library/blob/feat/kitcheng/rename/LICENSE) file for more details.
