# Garrard.GitLab

Garrard.GitLab is a .NET library that provides operations for working with GitLab projects.

## Installation

To install `Garrard.GitLab`, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```powershell
Install-Package Garrard.GitLab -Version 0.0.4
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="0.0.4" />
```

Or user the dotnet add command:

```powershell
dotnet add package Garrard.GitLab --version 0.0.3
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

        var projectHasBeenCreated = await GitOperations.CreateGitLabProject("new-project-name", "your-gitlab-pat", "domain-name-of-gitlab-instance");
        
        if (projectHasBeenCreated.IsFailure)
        {
            Console.Writeline($"{projectHasBeenCreated.Error}. Exiting...");
            return;
        }
        
        GitOperations.DownloadGitRepository(repoUrl, clonePath);
        GitOperations.CloneGitLabProject("https://github.com/yourusername/your-repo.git", "/path/to/clone");
        FileOperations.CopyFiles(clonePath, "/path/to/copy/to");
        GitOperations.CommitAndPushChanges("/path/to/commit/from", "commit message");
        FileOperations.RemoveTmpFolder("/path/to/remove"); 
    }
}
```

## Features

- Clone Git repositories
- Create branches
- Commit changes
- Copy files from cloned repo (or Project) to your new GitLab project folder (excluding the .git/ folder)
- Push changes
- Fetch and pull updates
- Remove temporary folder

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/garrardkitchen/gitlab-library/blob/feat/kitcheng/rename/LICENSE) file for more details.
