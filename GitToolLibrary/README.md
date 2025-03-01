# GitToolLibrary

GitToolLibrary is a .NET library that provides tools for working with Git repositories. It includes various utilities and functions to simplify common Git operations.

## Installation

To install GitToolLibrary, you can use the NuGet package manager. Run the following command in the Package Manager Console:

```
Install-Package GitToolLibrary -Version 1.0.0
```

Or add the following package reference to your project file:

```xml
<PackageReference Include="GitToolLibrary" Version="1.0.0" />
```

## Usage

Here is an example of how to use GitToolLibrary in your project:

```csharp
using GitToolLibrary;

class Program
{
    static async Task Main(string[] args)
    {
        // Example usage of GitToolLibrary

        var projectHasBeenCreated = await GitToolApi.CreateGitLabProject("new-project-name", "your-gitlab-pat", "domain-name-of-gitlab-instance");
        
        if (projectHasBeenCreated.IsFailure)
        {
            Console.Writeline($"{projectHasBeenCreated.Error}. Exiting...");
            return;
        }
        
        GitToolApi.DownloadGitRepository(repoUrl, clonePath);
        GitToolApi.CloneGitLabProject("https://github.com/yourusername/your-repo.git", "/path/to/clone");
        GitToolApi.CopyFiles(clonePath, "/path/to/copy/to");
        GitToolApi.CommitAndPushChanges("/path/to/commit/from", "commit message");
        GitToolApi.RemoveTmpFolder("/path/to/remove"); 
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
