# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Docker image** (`garrardkitchen/gitlab-mcp`): the MCP server is now published to [Docker Hub](https://hub.docker.com/r/garrardkitchen/gitlab-mcp) on every semver tag.
  - Multi-stage `Dockerfile` in `src/Garrard.GitLab.McpServer/` based on `mcr.microsoft.com/dotnet/aspnet:10.0`.
  - New GitHub Actions workflow (`.github/workflows/docker.yml`) builds and pushes `garrardkitchen/gitlab-mcp:<version>` and `:latest`. Requires `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN` secrets in the `docker-production` environment.
  - **stdio transport** (Claude Desktop): `docker run --rm -i -e GL_PAT=... -e GL_DOMAIN=... garrardkitchen/gitlab-mcp:latest`
  - **HTTP streaming transport**: `docker run --rm -p 8080:8080 -e MCP_TRANSPORT=http -e MCP_API_KEY=... -e GL_PAT=... -e GL_DOMAIN=... garrardkitchen/gitlab-mcp:latest`
- Updated `README.md` with Docker Hub badge, Docker usage examples, and CI/CD section.

## [1.0.2] - 2026-03-01

### Added
- **`ProjectClient.CreateProjectAccessToken`**: creates a project access token via the GitLab API.
  - Default name `GL_TOKEN`, default scope `WriteRepository`, default access level `Maintainer` (40), default expiry one year from creation.
  - `ProjectAccessTokenScope` flags enum with 14 scopes (`Api`, `ReadApi`, `WriteRepository`, `ReadRegistry`, `WriteRegistry`, `ReadPackageRegistry`, `WritePackageRegistry`, `CreateRunner`, `ManageRunner`, `AiFeatures`, `K8sProxy`, `ReadObservability`, `WriteObservability`).
  - `AccessLevel` enum: `NoAccess(0)`, `Minimal(5)`, `Guest(10)`, `Reporter(20)`, `Developer(30)`, `Maintainer(40)`, `Owner(50)`.
  - `GitLabProjectAccessToken` DTO: `Id`, `Name`, `Token` (creation only), `Scopes[]`, `AccessLevel`, `ExpiresAt`, `CreatedAt`, `Revoked`, `Active`.
  - New MCP tool `gitlab_create_project_access_token`.
- **`isHidden` parameter on `CreateOrUpdateProjectVariable`** (both library and MCP tool): sends the `hidden` field to the GitLab API, defaults to `true`. Hidden variable values are not retrievable after creation.
  - `GitLabVariable` DTO updated with `Hidden` property.
- 5 new unit tests covering the above.

### Changed
- `Garrard.GitLab.Library.Enums` namespace introduced for `AccessLevel` and `ProjectAccessTokenScope`.

## [1.0.2] - 2026-03-01

### Added
- **`ProjectClient.CreateProjectAccessToken`**: creates a GitLab project access token with configurable name, scopes, access level and expiry.
  - New `AccessLevel` enum: `NoAccess / Minimal / Guest / Reporter / Developer / Maintainer / Owner`
  - New `ProjectAccessTokenScope` flags enum: 14 scopes (`Api`, `ReadApi`, `WriteRepository`, `ReadRegistry`, etc.)
  - New `GitLabProjectAccessToken` DTO: `Id`, `Name`, `Token` (only on creation), `Scopes[]`, `AccessLevel`, `ExpiresAt`, `Active`, `Revoked`
  - Defaults: name `GL_TOKEN`, scope `WriteRepository`, access level `Maintainer (40)`, expiry +1 year
- **`isHidden` parameter** on `ProjectClient.CreateOrUpdateProjectVariable` (default `true`): sends `hidden: true` to the GitLab API so the variable value is concealed after creation.
- **`Hidden` property** added to `GitLabVariable` DTO.
- **MCP tools**: `gitlab_create_project_access_token` (accepts comma-separated scope string + int access level); `gitlab_create_or_update_project_variable` now includes `isHidden` parameter.
- 5 new unit tests covering the above.

### Changed
- `CreateOrUpdateProjectVariable` signature: `isHidden = true` inserted before `Action<string>? onMessage` (non-breaking for callers using named parameters).

## [1.0.0] - 2025-10-25

### Added
- **MCP Server** (`src/Garrard.GitLab.McpServer`): new ASP.NET Core + console hybrid server exposing all 25 library operations as [Model Context Protocol](https://modelcontextprotocol.io/) tools.
  - Supports both `stdio` (default, for Claude Desktop) and `http` transports via `MCP_TRANSPORT` env var.
  - HTTP transport includes API-key middleware (`Authorization: Bearer <MCP_API_KEY>`).
  - Per-tool `pat`/`gitlabDomain` parameter overrides with fallback to `GL_PAT`/`GL_DOMAIN` env vars.
  - Uses `ModelContextProtocol` 1.0.0 SDK.
- **Dependency Injection**: added `AddGarrardGitLab()` extension method and `IGitLabHttpClientFactory` abstraction for testability.
- **Unit tests** (`tests/Garrard.GitLab.Tests`): 36 tests using xUnit v3 + Moq covering all library operations and DI types.
- **MCP server tests** (`tests/Garrard.GitLab.McpServer.Tests`): 14 tests covering `ApiKeyMiddleware` and `ToolHelper`.
- **CI improvements**: upgraded to `actions/checkout@v4` + `actions/setup-dotnet@v4`, added NuGet cache, test result publishing via `EnricoMi/publish-unit-test-result-action@v2`, code coverage reporting via `irongut/CodeCoverageSummary@v1.3.0`, PR coverage comments, and a coverage threshold gate.
- **Publish workflow**: now triggers on semver tags (`v*`) rather than `main` push; includes test gate before publish; uses `nuget-production` GitHub Environment.

### Changed
- Repository structure: all source projects moved into `src/`; tests in `tests/`.
- Solution file updated to reference new paths.
- Library version bumped to `1.0.0`.
- CI no longer builds/packs against old root paths.


- Updated version to 0.0.21

## [2025-03-29]
- Updated version to 0.0.19

### Added
- Added new GitLab API group search methods:
  - FindGroups: Find GitLab groups by exact name or ID
  - SearchGroups: Search for GitLab groups using a wildcard pattern
- Both methods exclude groups marked for deletion

## [2025-03-27]

- Added new GitLab API features:
  - Get subgroups beneath a specified group with subgroup detection
  - Get projects within a group with detailed project information
  - Get project variables with automatic pagination
  - Get specific project variable by key and environment
  - Create or update project variables
  - Delete project variables
- Enhanced API methods to automatically handle pagination
- Added support for sorting and ordering in API methods
- Added comprehensive documentation for all new features
- Excludes Groups and Projects marked for deletion

## [2025-03-26]

####
- Updated version to 0.0.18 and added new feat to look for a placeholder in a file and replace it's value

## [2025-03-19]

####
- Updated version to 0.0.17 and added new feat to get a GitLab Group variable and create/update a Group Variable

## [2025-03-18]

#### Fixed
- Updated version to 0.0.16 and enhance project name existence check logic.

## [2025-03-07]

#### Added
- Updated version to 0.0.15 and can transfer project to another group or namespace 

## [2025-03-06]

### Added
- Updated version to 0.0.14 and enhanced error handling for unauthorized access with Personal Access Token
- Added functionality to create a file with content, and pushed initial commit, incremented version to 0.0.13
- Updated GitLab project creation to return project details such as Id and HttpUrlToRepo, and incremented version to 0.0.12
- Updated Git operations to support Personal Access Token (PAT) for repository access and incremented version to 0.0.11

### Documentation
- Updated CI and publish workflows to ignore changes in README.md and enhanced documentation
- Enhanced Git operations to support Personal Access Token (PAT) in DownloadGitRepository and CloneGitLabProject methods

## [2025-03-02]

### Added
- Updated version to 0.0.10 and fixed typos in README and code comments
- Updated requirements to reflect implemented actions and solution structure for GitLab project management
- Updated version to 0.0.9 and enhanced CreateGitLabProject to support optional group ID
- Updated version to 0.0.8 and enhanced DownloadGitRepository to support optional branch name
- Refactored Git operations to support branch creation and updated version to 0.0.7
- Enhanced project creation logic to handle existing project names and updated version to 0.0.6
- Updated installation command and example usage in README for version 0.0.5
- Updated CI workflow to trigger on feature branches instead of excluding main branch
- Bumped version to 0.0.4 and updated installation command in README
- Updated CI workflow to exclude main branch from push events
- Updated project name and description in README and .csproj for clarity
- Specified PowerShell syntax for installation command in README
- Updated README to reflect package name change from GitToolLibrary to Garrard.GitLab
- Added CI and publish workflows, restructured project files, and implemented file operations for GitLab integration
- Updated README and GitToolLibrary project version to 0.0.3 with new async methods and improved packaging
- Updated GitToolLibrary project version to 0.0.2 and enhanced packaging configuration
- Updated GitToolLibrary project configuration for NuGet publishing and changed package ID
- Updated project metadata in GitToolLibrary.csproj for versioning and description
- Updated README to clarify project structure and library usage
- Added Library with NuGet publishing workflow and updated project metadata
- Added CSharpFunctionalExtensions package and refactored GitToolApi methods for improved error handling
- Implemented user secrets configuration and enhanced project setup workflow
- Added Library for GitLab project management and repository operations
- Created a console app to create GL Project, cloned from a different repo to have this code committed and pushed to the new GL Project
