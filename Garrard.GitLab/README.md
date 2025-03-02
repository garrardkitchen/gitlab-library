# Garrard.GitLab

Garrard.GitLab is a .NET library that provides operations for working with GitLab projects.

## Installation

To install `Garrard.GitLab`, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```powershell
Install-Package Garrard.GitLab -Version 0.0.5
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="0.0.5" />
```

Or user the dotnet add command:

```powershell
dotnet add package Garrard.GitLab --version 0.0.5
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

        var projectCreation = await GitOperations.CreateGitLabProject("new-project-name", "your-gitlab-pat", "gitlab-domain");
        
        if (projectCreation.IsFailure)
        {
            Console.Writeline($"{projectCreation.Error}. Exiting...");
            return;
        }
        
        GitOperations.DownloadGitRepository("https://github.com/yourusername/your-repo.git", "/path/to/download/to");
        GitOperations.CloneGitLabProject("https://gitlab.com/yourusername/your-project.git", "/path/to/clone");
        FileOperations.CopyFiles("/path/to/download/to", "/path/to/clone");
        GitOperations.CommitAndPushChanges("/path/to/clone", "commit message");
        FileOperations.RemoveTmpFolder("/path/to/download/to"); 
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
