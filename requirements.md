# Actions Implemented

- Using C# and .NET 9.0, created a console application
- Used Spectre.Console for prompting user input
- Prompted the user for the GitLab project to download
- Prompted the user for a name to create a new GitLab project
- Wrote code that creates a new GitLab project
- Wrote code that downloads a git repository from a different GitLab project
- Wrote code to copy the files from the downloaded GitLab project into the local repo of the created GitLab project
- Created necessary paths if they don't exist
- Wrote code that performs a git commit and push using Linux process to execute git commands

# Solution Structure

The solution consists of the following projects:

- `Garrard.GitLab`: A .NET library that provides operations for working with GitLab projects.
- `Garrard.GitLab.Sample`: A sample console application demonstrating the usage of the `Garrard.GitLab` library.

The solution is built using C# and targets .NET 9.0.

# Features Implemented

- Console application using C# and .NET 9.0
- User input prompts using Spectre.Console
- Prompt for GitLab project to download
- Prompt for a name to create a new GitLab project
- Create a new GitLab project
- Download a Git repository from a different GitLab project
- Copy files from the downloaded GitLab project into the local repo of the created GitLab project
- Create necessary paths if they don't exist
- Perform a git commit and push using Linux process to execute git commands
