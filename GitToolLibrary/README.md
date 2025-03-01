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
    static void Main(string[] args)
    {
        // Example usage of GitToolLibrary
        var gitTool = new GitToolApi();
        gitTool.CloneRepository("https://github.com/yourusername/your-repo.git", "/path/to/clone");
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
