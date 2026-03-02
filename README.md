# Garrard.GitLab

A .NET 10 library and MCP server for automating GitLab operations — creating groups/projects, managing variables, cloning repositories, and more.

[![CI](https://github.com/kitcheng/gitlab-library/actions/workflows/ci.yml/badge.svg)](https://github.com/kitcheng/gitlab-library/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Garrard.GitLab.svg)](https://www.nuget.org/packages/Garrard.GitLab)
[![Docker Hub](https://img.shields.io/docker/v/garrardkitchen/gitlab-mcp?label=Docker%20Hub)](https://hub.docker.com/r/garrardkitchen/gitlab-mcp)

---

## Repository Structure

```
git-tool.sln
src/
  Garrard.GitLab/              ← .NET library (NuGet package)
  Garrard.GitLab.Sample/       ← Sample console app
  Garrard.GitLab.McpServer/    ← MCP server (stdio & HTTP)
    Dockerfile                 ← Docker image build
tests/
  Garrard.GitLab.Tests/        ← Library unit tests (xUnit v3)
  Garrard.GitLab.McpServer.Tests/ ← MCP server unit tests
.github/workflows/
  ci.yml                       ← Build, test, coverage on PRs
  publish.yml                  ← Publish to NuGet on semver tags
  docker.yml                   ← Build & push Docker image on semver tags
```

---

## NuGet Library

### Installation

```bash
dotnet add package Garrard.GitLab
```

### Usage

```csharp
using Garrard.GitLab.Library;
using Garrard.GitLab.Library.Enums;

// Register once at startup
services.AddGarrardGitLab(opts =>
{
    opts.Pat    = "glpat-xxxx";   // required
    opts.Domain = "gitlab.com";   // optional, defaults to gitlab.com
});

// Inject and use
public class MyService(ProjectClient projectClient, GroupClient groupClient)
{
    public async Task Run()
    {
        // Find a group
        var groups = await groupClient.FindGroups("my-team");

        // Search projects by partial name or namespace (paginated)
        var search = await projectClient.SearchProjects(search: "my-app", page: 1, perPage: 20);
        if (search.IsSuccess)
        {
            Console.WriteLine($"Page {search.Value.Page}/{search.Value.TotalPages} — {search.Value.TotalItems} total");
            foreach (var p in search.Value.Items)
                Console.WriteLine($"  {p.PathWithNamespace} (ID: {p.Id})");
        }

        // Look up a project by exact ID
        var byId = await projectClient.SearchProjects(id: 99);

        // Create a project access token
        var token = await projectClient.CreateProjectAccessToken(
            "99",
            scopes: ProjectAccessTokenScope.WriteRepository | ProjectAccessTokenScope.ReadApi,
            accessLevel: AccessLevel.Maintainer);

        // Create a hidden project variable (value concealed after creation)
        await projectClient.CreateOrUpdateProjectVariable("99", "API_KEY", "secret");
    }
}
```

See [`src/Garrard.GitLab/README.md`](src/Garrard.GitLab/README.md) for full API documentation.

---

## MCP Server

The MCP server exposes all library operations as [Model Context Protocol](https://modelcontextprotocol.io/) tools, allowing AI assistants (Claude, Copilot, etc.) to manage GitLab programmatically.

### Transport modes

| Mode  | How to run | Use case |
|-------|-----------|----------|
| stdio | Default (no env var) | Claude Desktop, local AI tools |
| HTTP  | `MCP_TRANSPORT=http` | Remote/multi-client deployment |

### Environment variables

| Variable | Required | Description |
|----------|----------|-------------|
| `GL_PAT` | Yes | GitLab Personal Access Token (default for all tools) |
| `GL_DOMAIN` | Yes | GitLab domain (e.g. `gitlab.com`) |
| `MCP_TRANSPORT` | No | Set to `http` to enable HTTP/SSE transport |
| `MCP_API_KEY` | No | When HTTP mode is enabled, require this Bearer token on all requests |

### Running (stdio — Claude Desktop)

```jsonc
// Claude Desktop mcp config
{
  "mcpServers": {
    "gitlab": {
      "command": "dotnet",
      "args": ["run", "--project", "src/Garrard.GitLab.McpServer"],
      "env": {
        "GL_PAT": "glpat-xxxx",
        "GL_DOMAIN": "gitlab.com"
      }
    }
  }
}
```

### Running (HTTP)

```bash
MCP_TRANSPORT=http MCP_API_KEY=my-secret GL_PAT=glpat-xxxx GL_DOMAIN=gitlab.com \
  dotnet run --project src/Garrard.GitLab.McpServer
```

### Running with Docker

The MCP server is published as a Docker image to [Docker Hub (`garrardkitchen/gitlab-mcp`)](https://hub.docker.com/r/garrardkitchen/gitlab-mcp) on every semver tag.

#### stdio transport (Claude Desktop)

Pass your PAT and domain as environment variables. The `-i` flag keeps stdin open for MCP protocol communication.

```jsonc
// Claude Desktop mcp config (~/.config/claude/claude_desktop_config.json)
{
  "mcpServers": {
    "gitlab": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-e", "GL_PAT=glpat-xxxx",
        "-e", "GL_DOMAIN=gitlab.com",
        "garrardkitchen/gitlab-mcp:latest"
      ]
    }
  }
}
```

#### HTTP streaming transport

Set `MCP_TRANSPORT=http` and publish port `8080`. Optionally protect the endpoint with `MCP_API_KEY`.

```bash
docker run --rm -p 8080:8080 \
  -e MCP_TRANSPORT=http \
  -e MCP_API_KEY=my-secret \
  -e GL_PAT=glpat-xxxx \
  -e GL_DOMAIN=gitlab.com \
  garrardkitchen/gitlab-mcp:latest
```

The server is then reachable at `http://localhost:8080/mcp`. Pass `Authorization: Bearer my-secret` on all requests when `MCP_API_KEY` is set.

---

### Available MCP Tools (26 tools)

| Category | Tool Name | Description |
|----------|-----------|-------------|
| Groups | `gitlab_get_subgroups` | Get subgroups under a parent group |
| Groups | `gitlab_find_groups` | Find groups by exact name or ID |
| Groups | `gitlab_search_groups` | Search groups with wildcard pattern |
| Groups | `gitlab_create_group` | Create a new GitLab group |
| Projects | `gitlab_search_projects` | Find projects by partial name/namespace or exact ID (paginated) |
| Projects | `gitlab_get_projects_in_group` | List projects in a group |
| Projects | `gitlab_get_project_variables` | Get all variables for a project |
| Projects | `gitlab_get_project_variable` | Get a specific project variable |
| Projects | `gitlab_create_or_update_project_variable` | Create or update a project variable (hidden by default) |
| Projects | `gitlab_delete_project_variable` | Delete a project variable |
| Projects | `gitlab_create_project` | Create a new GitLab project |
| Projects | `gitlab_create_project_access_token` | Create a project access token with configurable scopes and access level |
| Group Variables | `gitlab_get_group_variable` | Get a specific group variable |
| Group Variables | `gitlab_create_or_update_group_variable` | Create or update a group variable |
| Summary | `gitlab_get_group_summary` | Get summary stats for a group |
| Summary | `gitlab_get_project_summary` | Get summary stats for a project |
| Summary | `gitlab_get_group_projects_summary` | Get summary for all projects in a group |
| Git | `gitlab_download_repository` | Clone/download a GitLab repository |
| Git | `gitlab_create_branch` | Create a new branch in a local repo |
| Git | `gitlab_push_changes` | Stage, commit, and push changes |
| Git | `gitlab_branch_commit_push` | Create branch, commit, and push in one step |
| Git | `gitlab_clone_project` | Clone a GitLab project |
| Git | `gitlab_transfer_project` | Transfer a project to another namespace |
| Files | `gitlab_remove_temp_folder` | Remove a temporary folder |
| Files | `gitlab_copy_files` | Copy files from source to destination |
| Files | `gitlab_create_file_with_content` | Create a file with given content |
| Files | `gitlab_replace_placeholder_in_file` | Replace a placeholder value in a file |

---

## Authentication & Security

### stdio transport
No additional auth — security comes from the process boundary. Suitable for local use with Claude Desktop.

### HTTP transport
- Requires `Authorization: Bearer <MCP_API_KEY>` header on every request.
- Return `401 Unauthorized` if the key is missing or invalid.
- Configure `MCP_API_KEY` as a secret — never commit it to source control.

### GitLab PAT
- PAT is configured once via `GitLabOptions.Pat` (from `GitLab__Pat` env var or `appsettings.json`).
- The PAT is injected into `HttpClient` at startup — no PAT is ever passed into method signatures.
- PAT values are never logged.
- Use a PAT with minimum required scopes for your use case.

---

## Running Tests

```bash
# All tests
dotnet test

# With coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Library tests only
dotnet test tests/Garrard.GitLab.Tests/

# MCP server tests only
dotnet test tests/Garrard.GitLab.McpServer.Tests/
```

---

## CI/CD

- **CI** triggers on `feat/**` and `fix/**` branches, and on pull requests to `main`.
- **Publish (NuGet)** triggers on semver tags (`v*`), e.g. `git tag v1.0.0 && git push --tags`.
- **Publish (Docker)** triggers on semver tags (`v*`) — builds and pushes `garrardkitchen/gitlab-mcp:<version>` and `:latest` to [Docker Hub](https://hub.docker.com/r/garrardkitchen/gitlab-mcp). Requires `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets in the `docker-production` GitHub Environment.
- Test results and code coverage summaries are posted as PR comments.
- Build fails if line coverage drops below 60% (warn) / 80% (fail).

### Recommended improvements (CD best practices)
- Pin action versions to SHA for supply-chain security.
- Add CodeQL scanning on PRs.
- Enable Dependabot for NuGet and Actions.
- Use GitHub Environments with required reviewers for publish.
- Adopt OIDC trusted publishing to eliminate the `NUGET_API_KEY` secret.
- Add SBOM generation on release via `anchore/sbom-action`.
