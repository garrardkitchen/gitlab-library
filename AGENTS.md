# Copilot Instructions

## Build & Test Commands

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/Garrard.GitLab.Tests/
dotnet test tests/Garrard.GitLab.McpServer.Tests/

# Run a single test by name (filter)
dotnet test tests/Garrard.GitLab.Tests/ --filter "FullyQualifiedName~FindGroups_ById_ReturnsGroup"

# Run tests with coverage (Cobertura format, matches CI)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

# Pack the NuGet library
dotnet pack src/Garrard.GitLab/Garrard.GitLab.csproj --configuration Release
```

All projects target **net10.0**. The library builds to a NuGet package on every build (`GeneratePackageOnBuild=true`).

## Architecture

Three deployable units live under `src/`, two test projects under `tests/`:

```
src/
  Garrard.GitLab/            ← NuGet library (PackageId: Garrard.GitLab)
  Garrard.GitLab.McpServer/  ← MCP server (stdio + HTTP transports)
  Garrard.GitLab.Sample/     ← Console sample showing library usage
tests/
  Garrard.GitLab.Tests/            ← 44 unit tests for the library
  Garrard.GitLab.McpServer.Tests/  ← 10 unit tests for the MCP server
```

### Library (`Garrard.GitLab.Library` namespace, package `Garrard.GitLab`)

The library is DI-first. Callers register everything with one call:

```csharp
services.AddGarrardGitLab(opts => {
    opts.Pat = "<gitlab-pat>";      // required
    opts.Domain = "gitlab.com";     // optional, defaults to gitlab.com
});
```

Six instance **client classes** map to GitLab API domains:

| Class | Responsibility |
|---|---|
| `GroupClient` | Subgroups, find, search, create |
| `ProjectClient` | Projects, variables, transfer |
| `GroupVariableClient` | Group-level CI/CD variables |
| `SummaryClient` | Composite summaries (delegates to GroupClient + ProjectClient) |
| `GitClient` | Git-level operations (clone URL, default branch) |
| `FileClient` | Local file placeholder replacement — **no HTTP, no DI** |

All clients except `FileClient` take `IGitLabHttpClientFactory` + `IOptions<GitLabOptions>` in their constructor. `SummaryClient` takes `GroupClient` + `ProjectClient` instead.

`AddGarrardGitLab()` registers all six clients as **singletons** and a named `HttpClient("gitlab")` with Bearer auth and base address pre-configured. No PAT is ever passed into a method — it comes from `GitLabOptions`.

DTOs live in `DTOs/`, internal request serialization models in `Requests/`. All public methods return `CSharpFunctionalExtensions.Result<T>` — never throw, never return null.

### MCP Server

The MCP server exposes all library operations as AI tools. Transport is selected at startup via the `MCP_TRANSPORT` environment variable:
- `MCP_TRANSPORT=http` → `WebApplication` + `app.MapMcp()` + `ApiKeyMiddleware`
- anything else (default) → stdio transport, all logs redirected to **stderr** to keep stdout clean for MCP protocol

PAT is configured via either `GitLab__Pat` / `GitLab__Domain` (standard .NET env var binding) or the legacy aliases `GL_PAT` / `GL_DOMAIN`. The server maps legacy → canonical in `Program.cs` before binding options.

Tool classes (`GroupTools`, `ProjectTools`, etc.) are **sealed non-static classes** registered in DI, each injecting its specific client (e.g. `GroupTools(GroupClient groupClient)`). They are discovered automatically by `WithToolsFromAssembly(typeof(Program).Assembly)`. HTTP transport additionally enforces `Authorization: Bearer <MCP_API_KEY>` when `MCP_API_KEY` is configured (no-op if absent).

## Key Conventions

### Result pattern
Every async library method returns `Task<Result<T>>` (from `CSharpFunctionalExtensions`). Never throw from a public method — catch exceptions and return `Result.Failure<T>(ex.Message)`. Check `result.IsSuccess` / `result.Value` / `result.Error` at call sites.

### Namespace vs package name
- **Namespace**: `Garrard.GitLab.Library` (and sub-namespaces like `Garrard.GitLab.Library.DTOs`, `Garrard.GitLab.Library.Http`)
- **NuGet package ID**: `Garrard.GitLab` (unchanged)
- Consumers `dotnet add package Garrard.GitLab` but write `using Garrard.GitLab.Library;`

### URL construction
Clients construct **full absolute URLs** (`$"https://{_opts.Domain}/api/v4/..."`) rather than relative URLs. The HttpClient's `BaseAddress` is set in `AddGarrardGitLab()` but is intentionally bypassed in client methods so the configured domain is always authoritative.

### Pagination
All list-returning methods page through the GitLab API using `per_page=100`, checking the `X-Total-Pages` header to detect end-of-results. Always filter out `marked_for_deletion_on` entries (`IsMarkedForDeletion`).

### MCP tool naming
Tool names follow `gitlab_<verb>_<noun>` snake_case (e.g. `gitlab_get_subgroups`, `gitlab_create_project`). Every tool method and parameter must carry a `[Description("...")]` attribute for AI discoverability.

### Testing
- Tests use **xUnit v3** (`xunit.v3` 1.1.0 package — not `xunit`) + **Moq**.
- Use `HttpTestHelpers.CreateMockFactory(statusCode, json)` to get a `Mock<IGitLabHttpClientFactory>` backed by a mock handler. Use `CreateRealClientFactory()` only for "invalid domain" failure path tests.
- Construct clients under test with `Options.Create(new GitLabOptions { Pat = "test-pat", Domain = "gitlab.example.com" })`.
- MCP server middleware tests use `HostBuilder + UseTestServer()` (not `WebApplicationFactory<Program>`) to avoid triggering the transport-branching `Program.cs` startup.
- `SummaryClient` tests inject real `GroupClient` / `ProjectClient` instances backed by mock factories.

### CI/CD
- CI triggers on `feat/**` and `fix/**` branches, and on PRs to `main`.
- Coverage threshold: 60% minimum line, 80% target (enforced by `irongut/CodeCoverageSummary`).
- Publish to NuGet is triggered by semver tags (`v*`) and requires passing tests + the `nuget-production` GitHub Environment.
