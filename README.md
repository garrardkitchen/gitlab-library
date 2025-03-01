# gitlab-library

This code is a .NET 9.0 console application that performs the following actions:

1. Prompt the user for input:

- GitLab repository URL to download.
- Local path to clone the repository.
- Commit message.
- Name for the new GitLab project.

2. Create a new GitLab project:

- Uses the GitLab API to create a new project with the specified name.
- Prompts the user for their GitLab private token and domain.
- If the project creation is successful, it proceeds; otherwise, it exits.

3. Download a Git repository from GitLab:

- Clones the specified GitLab repository to the local path provided by the user.

4. Clone the new GitLab project:

- Clones the newly created GitLab project to a specified local path.

5. Copy files from the downloaded repository to the new project:

- Copies all files and directories from the downloaded repository to the new project's local path.
- Skips copying .git directories and files.

6. Perform a git commit and push:

- Adds all changes to the new project's local repository.
- Commits the changes with the specified commit message.
- Pushes the changes to the new GitLab project repository.

The code uses the `Spectre.Console` library for prompting user input and displaying messages in the console. It also uses the `HttpClient` class to interact with the GitLab API and the `Process` class to execute git commands.

This repo contains a sample console app that calls upon the GitToolLibrary to perform the appropriate actions.