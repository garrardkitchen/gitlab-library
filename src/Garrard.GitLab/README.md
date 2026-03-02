# Garrard.GitLab

A .NET library for working with GitLab groups, projects, variables, and repositories via the GitLab REST API.

## Installation

```bash
dotnet add package Garrard.GitLab
```

Install a specific version:

```bash
dotnet add package Garrard.GitLab --version 1.0.3
```

File-based Apps:

```bash
#:package Garrard.GitLab@1.0.3
```

Or add directly to your project file:

```xml
<PackageReference Include="Garrard.GitLab" Version="1.0.3" />
```

## Quick Start

Register the library with your DI container once at startup:

```csharp
using Garrard.GitLab.Library;
using Garrard.GitLab.Library.Enums;  // AccessLevel, ProjectAccessTokenScope

services.AddGarrardGitLab(opts =>
{
    opts.Pat    = "your-gitlab-personal-access-token";  // required
    opts.Domain = "gitlab.com";                         // optional, defaults to gitlab.com
});
```

Then inject the client(s) you need:

```csharp
public class MyService(GroupClient groupClient, ProjectClient projectClient)
{
    public async Task Run()
    {
        var result = await groupClient.FindGroups("my-team");

        if (result.IsSuccess)
        {
            foreach (var group in result.Value)
                Console.WriteLine($"{group.Name} ({group.Id})");
        }
        else
        {
            Console.WriteLine($"Error: {result.Error}");
        }
    }
}
```

## Configuration

Options are bound from the `GitLab` configuration section. Using `appsettings.json`:

```json
{
  "GitLab": {
    "Pat": "glpat-xxxxxxxxxxxxxxxxxxxx",
    "Domain": "gitlab.mycompany.com"
  }
}
```

Or environment variables:

```bash
GitLab__Pat=glpat-xxxxxxxxxxxxxxxxxxxx
GitLab__Domain=gitlab.mycompany.com
```

## Clients

All methods return `Task<Result<T>>` from [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) — they never throw. Check `result.IsSuccess`, `result.Value`, and `result.Error`.

### GroupClient

```csharp
// Find groups by exact name or ID
var groups = await groupClient.FindGroups("my-team");

// Search groups using a partial/wildcard pattern
var matches = await groupClient.SearchGroups("team-");

// Get all subgroups beneath a group
var subgroups = await groupClient.GetSubgroups("parent-group");

// Create a top-level group
var newGroup = await groupClient.CreateGitLabGroup("My New Team");

// Create a subgroup under an existing group
var subgroup = await groupClient.CreateGitLabGroup("Backend", parentId: 42);
```

### ProjectClient

```csharp
// Get all projects in a group (includes subgroups by default)
var projects = await projectClient.GetProjectsInGroup("my-team");

// Create a new project (optionally in a group)
var project = await projectClient.CreateGitLabProject("my-app", groupId: 42);

// Transfer a project to a different group
var transfer = await projectClient.TransferProjectToGroupOrNamespace(projectId: 99, "target-group");

// Project variables (isHidden: true by default — value hidden after creation)
var vars      = await projectClient.GetProjectVariables(projectId: 99);
var variable  = await projectClient.GetProjectVariable(99, "API_KEY");
var upserted  = await projectClient.CreateOrUpdateProjectVariable(99, "API_KEY", "secret");
var deleted   = await projectClient.DeleteProjectVariable(99, "API_KEY");

// Search projects by partial name or namespace (paginated)
var page1 = await projectClient.SearchProjects(search: "my-app", page: 1, perPage: 20);
if (page1.IsSuccess)
{
    Console.WriteLine($"Page {page1.Value.Page} of {page1.Value.TotalPages} ({page1.Value.TotalItems} total)");
    foreach (var p in page1.Value.Items)
        Console.WriteLine($"  {p.PathWithNamespace} (ID: {p.Id})");
}

// Look up a project by exact ID
var byId = await projectClient.SearchProjects(id: 99);

// Create a project access token
var tokenResult = await projectClient.CreateProjectAccessToken(
    projectId:   "99",
    name:        "GL_TOKEN",                                 // default
    scopes:      ProjectAccessTokenScope.WriteRepository,    // default
    accessLevel: AccessLevel.Maintainer,                     // default (40)
    expiresAt:   new DateOnly(2027, 1, 1));                  // default: +1 year

if (tokenResult.IsSuccess)
{
    Console.WriteLine($"Token: {tokenResult.Value.Token}");
    Console.WriteLine($"Expires: {tokenResult.Value.ExpiresAt}");
    Console.WriteLine($"Access level: {tokenResult.Value.AccessLevel}");
    Console.WriteLine($"Scopes: {string.Join(", ", tokenResult.Value.Scopes)}");
}
```

#### `ProjectAccessTokenScope` (flags enum)

| Value | API scope string |
|---|---|
| `Api` | `api` |
| `ReadApi` | `read_api` |
| `ReadRepository` | `read_repository` |
| `WriteRepository` | `write_repository` |
| `ReadRegistry` | `read_registry` |
| `WriteRegistry` | `write_registry` |
| `ReadPackageRegistry` | `read_package_registry` |
| `WritePackageRegistry` | `write_package_registry` |
| `CreateRunner` | `create_runner` |
| `ManageRunner` | `manage_runner` |
| `AiFeatures` | `ai_features` |
| `K8sProxy` | `k8s_proxy` |
| `ReadObservability` | `read_observability` |
| `WriteObservability` | `write_observability` |

Combine scopes with `|`:
```csharp
ProjectAccessTokenScope.WriteRepository | ProjectAccessTokenScope.ReadApi
```

#### `AccessLevel` enum

| Value | Level |
|---|---|
| `Guest` | 10 |
| `Reporter` | 20 |
| `Developer` | 30 |
| `Maintainer` | 40 (default) |
| `Owner` | 50 |

### GroupVariableClient

```csharp
// Get a group-level CI/CD variable
var variable = await groupVariableClient.GetGroupVariable("1607", "DEPLOY_TOKEN");

// Create or update a group variable
var result = await groupVariableClient.CreateOrUpdateGroupVariable(
    groupId:          "1607",
    key:              "DEPLOY_TOKEN",
    value:            "my-secret",
    variableType:     "env_var",   // optional
    isProtected:      false,       // optional
    isMasked:         true,        // optional
    environmentScope: "*"          // optional
);
```

### SummaryClient

```csharp
// Overview of a group (subgroup count, project count)
var groupSummary = await summaryClient.GetGroupSummary("my-team");

// Summaries of all projects within a group
var projectSummaries = await summaryClient.GetGroupProjectsSummary("my-team", includeSubgroups: true);

// Summary of a single project (variable count etc.)
var projectSummary = await summaryClient.GetProjectSummary(projectId: 99);
```

### GitClient

```csharp
// Create and clone a new GitLab project, then push an initial commit
var project = await gitClient.CreateGitLabProject("my-app", groupId: 42);
gitClient.DownloadGitRepository("https://github.com/org/template.git", "/tmp/template", "main");
gitClient.CloneGitLabProject(project.Value.HttpUrlToRepo, "/tmp/new-project");
gitClient.BranchCommitPushChanges("/tmp/new-project", "initial commit", "main");

// Transfer a project
await gitClient.TransferProjectToGroupOrNamespace(project.Value.Id, "target-group");
```

### FileClient

`FileClient` has no dependencies and can be instantiated directly (`new FileClient()`) or injected.

```csharp
// Create a file
fileClient.CreateFileWithContent("/tmp/new-project", "README.md", "# My App");

// Copy files between directories (excludes .git/)
fileClient.CopyFiles("/tmp/template", "/tmp/new-project");

// Replace a placeholder value in a file
var result = await fileClient.ReplacePlaceholderInFile(
    "./.gitlab-ci.yml",
    "WORKSPACE_NAME",
    "\"<enter-workload-name>\"",
    "\"my-workspace\"",
    ":"
);

// Clean up
fileClient.RemoveTempFolder("/tmp/template");
```

## Progress callbacks

All async methods accept an optional `Action<string>? onMessage` parameter for progress output:

```csharp
var result = await groupClient.GetSubgroups("my-team", onMessage: Console.WriteLine);
```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on [GitHub](https://github.com/garrardkitchen/gitlab-library).

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/garrardkitchen/gitlab-library/blob/main/LICENSE) file for details.

