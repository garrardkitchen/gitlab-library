# Garrard.GitLab

Garrard.GitLab is a .NET library that provides operations for working with GitLab projects.

## Installation

To install `Garrard.GitLab`, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```powershell
Install-Package Garrard.GitLab -Version 0.0.19
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="0.0.19" />
```

Or use the dotnet add command:

```powershell
dotnet add package Garrard.GitLab --version 0.0.19
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

        // Get a group variable
        
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

        // Search and replace example:
        
        FileOperations.CreateFileWithContent($"./", ".gitlab-ci.yml", $"TF_VAR_TFE_WORKSPACE_NAME: \"<enter-workload-name>\"");

        var replacePlaceholderInFile = await FileOperations.ReplacePlaceholderInFile("./.gitlab-ci.yml", "TF_VAR_TFE_WORKSPACE_NAME", "\"<enter-workload-name>\"", "\"foo\"", ":", Console.WriteLine);

        if (replacePlaceholderInFile.IsFailure) 
        {
            Console.WriteLine(replacePlaceholderInFile.Error);
        }
        
        // NEW API METHODS EXAMPLES
        
        // Get all subgroups for a group
        var subgroups = await GroupOperations.GetSubgroups(
            "my-group-name",     // Group ID or name
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
            "my-group-name",     // Group ID or name
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
        
        // Get all project variables
        var projectVars = await ProjectOperations.GetProjectVariables(
            projectCreation.Value.Id, // Project ID
            gitlabPat,               // Personal Access Token
            gitlabDomain,            // GitLab domain
            Console.WriteLine        // Optional message handler
        );
        
        if (projectVars.IsSuccess)
        {
            Console.WriteLine($"Found {projectVars.Value.Length} project variables");
            
            foreach (var projectVar in projectVars.Value)
            {
                Console.WriteLine($"Variable: {projectVar.Key}");
                Console.WriteLine($"  Value: {projectVar.Value}");
                Console.WriteLine($"  Type: {projectVar.VariableType}");
                Console.WriteLine($"  Environment: {projectVar.EnvironmentScope}");
            }
        }
        
        // Create or update a project variable
        var createVarResult = await ProjectOperations.CreateOrUpdateProjectVariable(
            projectCreation.Value.Id, // Project ID
            "API_KEY",               // Variable key
            "secret-value-123",      // Variable value
            gitlabPat,               // Personal Access Token
            gitlabDomain,            // GitLab domain
            "env_var",               // Variable type (optional, default: env_var)
            false,                   // Is protected (optional, default: false)
            true,                    // Is masked (optional, default: false)
            "production",            // Environment scope (optional, default: *)
            Console.WriteLine        // Optional message handler
        );
        
        if (createVarResult.IsSuccess)
        {
            Console.WriteLine($"Variable {createVarResult.Value.Key} created/updated successfully");
        }
        
        // Get a specific project variable
        var projectVar = await ProjectOperations.GetProjectVariable(
            projectCreation.Value.Id, // Project ID
            "API_KEY",               // Variable key
            gitlabPat,               // Personal Access Token
            gitlabDomain,            // GitLab domain
            "production",            // Environment scope (optional, default: *)
            Console.WriteLine        // Optional message handler
        );
        
        if (projectVar.IsSuccess)
        {
            Console.WriteLine($"Variable {projectVar.Value.Key} value: {projectVar.Value.Value}");
        }
        
        // Delete a project variable
        var deleteResult = await ProjectOperations.DeleteProjectVariable(
            projectCreation.Value.Id, // Project ID
            "API_KEY",               // Variable key
            gitlabPat,               // Personal Access Token
            gitlabDomain,            // GitLab domain
            "production",            // Environment scope (optional, default: *)
            Console.WriteLine        // Optional message handler
        );
        
        if (deleteResult.IsSuccess)
        {
            Console.WriteLine("Variable deleted successfully");
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
- Search for a placeholder in a file and replace its values
- Get all subgroups beneath a specific group
  - Works with both group IDs and names
  - Includes information about whether each subgroup has nested subgroups
  - Automatically retrieves all subgroups across multiple pages
  - Supports ordering and sorting
  - Excludes any marked for deletion
- Get all projects within a group
  - Works with both group IDs and names
  - Retrieves detailed project information including namespace data
  - Option to include projects from subgroups
  - Automatically retrieves all projects across multiple pages
  - Supports ordering and sorting
  - Excludes any marked for deletion
- Get all project variables
  - Automatically retrieves all variables across multiple pages
- Get a specific project variable
  - Supports filtering by environment scope
- Create or update a project variable
  - Supports variable type (env_var or file)
  - Options for protected and masked variables
  - Supports environment scope
- Delete a project variable
  - Supports deletion with specific environment scope

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/garrardkitchen/gitlab-library/blob/feat/kitcheng/rename/LICENSE) file for more details.
