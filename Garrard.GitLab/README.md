# GitToolLibrary

GitToolLibrary is a .NET library that provides tools for working with Git repositories. It includes various utilities and functions to simplify common Git operations.

## Installation

To install GitToolLibrary, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```powershell
Install-Package Garrard.GitLab -Version 0.0.4
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="0.0.4" />
```

## Usage

Here is an example of how to use GitToolLibrary in your project:

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
- Push changes
- Fetch and pull updates

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
