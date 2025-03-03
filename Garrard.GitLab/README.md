# Garrard.GitLab

Garrard.GitLab is a .NET library that provides operations for working with GitLab projects.

## Installation

To install `Garrard.GitLab`, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```powershell
Install-Package Garrard.GitLab -Version 0.0.12
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="0.0.12" />
```

Or use the dotnet add command:

```powershell
dotnet add package Garrard.GitLab --version 0.0.12
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
        
        // will use the new name (will have changed if couldn't use the original name)
        newProjectName = projectCreation.Value.Name;
        
        GitOperations.DownloadGitRepository("https://github.com/yourusername/your-repo.git", "/path/to/download/to", "branch-name", "pat");
        GitOperations.CloneGitLabProject(projectCreation.Value.HttpUrlToRepo, "/path/to/clone", "pat");
        FileOperations.CopyFiles("/path/to/download/to", "/path/to/clone");
        GitOperations.BranchCommitPushChanges("/path/to/clone", "commit message", "branch-name-or-omit-to-use-mainline-branch");
        FileOperations.RemoveTempFolder("/path/to/download/to"); 
    }
}
```

## Features

- Create a new GitLab project 
  - It will create a unique project (by incrementing a number after your suggested name) if your suggested name exists
  - It will by default add the project to your user's namespace. If you supply a groupID, it will
    add the project to this group instead
  - Returns a tuple of (Id, Name, HttpUrlToRepo and PathWithNamespace)
- Download an existing git repository 
  - You can provide branch name
  - You can provide a PAT (uses oauth2)
- Clone GitLab project
  - You can provide a PAT (uses oauth2)
- Copy files from cloned repo (or Project) to your new GitLab project folder (excluding the .git/ folder)
- Branch (optional), Commit and push changes
- Remove temporary folder

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/garrardkitchen/gitlab-library/blob/feat/kitcheng/rename/LICENSE) file for more details.
