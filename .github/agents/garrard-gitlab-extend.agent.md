---
name: garrard-gitlab-extend
description: Add a new library client method or MCP tool to the Garrard.GitLab repository following all project conventions. Use when asked to add GitLab API coverage, a new tool, or a new client method.
---

# Garrard.GitLab — Extend Library / MCP Tools

You are extending the Garrard.GitLab .NET library or its MCP server. Follow all conventions below precisely. Verify with builds and tests before presenting results.

## Repository layout

```
src/
  Garrard.GitLab/            ← NuGet library  (namespace: Garrard.GitLab.Library)
  Garrard.GitLab.McpServer/  ← MCP server     (namespace: Garrard.GitLab.McpServer)
tests/
  Garrard.GitLab.Tests/            ← Library unit tests
  Garrard.GitLab.McpServer.Tests/  ← MCP server unit tests
```

---

## Adding a new library client method

### 1. Identify the right client class

| Client | GitLab API area |
|---|---|
| `GroupClient` | Groups, subgroups |
| `ProjectClient` | Projects, project variables, transfer |
| `GroupVariableClient` | Group-level CI/CD variables |
| `SummaryClient` | Composite summaries (injects GroupClient + ProjectClient, no HTTP) |
| `GitClient` | Git operations (clone URLs, default branch) |
| `FileClient` | Local file operations — no HTTP, no DI |

### 2. Method signature rules

```csharp
public async Task<Result<TDto>> MethodName(
    string requiredParam,
    string optionalParam = "default",
    Action<string>? onMessage = null)   // optional progress callback
```

- **Always** return `Task<Result<T>>` from `CSharpFunctionalExtensions`.
- **Never** throw — wrap all logic in `try/catch` and return `Result.Failure<T>(ex.Message)`.
- **Never** add `pat` or `domain` parameters — use `_opts.Pat` / `_opts.Domain` from the injected `GitLabOptions`.
- Include `Action<string>? onMessage = null` when the operation emits progress messages.

### 3. HTTP calls

```csharp
var client = _factory.CreateClient();
var response = await client.GetAsync($"https://{_opts.Domain}/api/v4/...");
```

- Always construct **full absolute URLs** — never relative paths.
- Always call `Uri.EscapeDataString()` on any user-supplied string embedded in a URL.
- Paginate list endpoints: `per_page=100`, increment `page`, check `X-Total-Pages` response header.
- Filter out soft-deleted records: `.Where(x => !x.IsMarkedForDeletion)`.

### 4. DTOs

- Place new DTOs in `src/Garrard.GitLab/DTOs/`.
- Name them without `Dto` suffix — e.g. `GitLabPipeline`, not `GitLabPipelineDto`.
- Use `[JsonPropertyName("snake_case_field")]` for all JSON-mapped properties.
- Use `int` (not `short`) for all GitLab IDs — IDs routinely exceed 32,767.
- Shared shapes (e.g. a DTO used by both project and group endpoints) → one file.

### 5. Registration

Add singletons to `ServiceCollectionExtensions.cs` only when adding a **new** client class. Individual methods on existing clients need no registration change.

---

## Adding a new MCP tool method

### 1. Identify or create the tool class

Each tool class wraps exactly one client:

| Tool class | Client injected |
|---|---|
| `GroupTools` | `GroupClient` |
| `ProjectTools` | `ProjectClient` |
| `GroupVariableTools` | `GroupVariableClient` |
| `SummaryTools` | `SummaryClient` |
| `GitTools` | `GitClient` |
| `FileTools` | `FileClient` |

Tool classes live in `src/Garrard.GitLab.McpServer/Tools/`.

### 2. Tool class structure

```csharp
[McpServerToolType]
public sealed class GroupTools(GroupClient groupClient)
{
    [McpServerTool(Name = "gitlab_verb_noun"), Description("One sentence description.")]
    public async Task<string> MethodName(
        [Description("What this parameter does.")] string param1,
        [Description("Optional param description.")] string param2 = "default")
    {
        var result = await groupClient.MethodName(param1, param2);
        return ToolHelper.Serialize(result);
    }
}
```

- Tool class must be `public sealed` (non-static) — DI resolves it.
- Tool naming: **`gitlab_<verb>_<noun>`** snake_case — e.g. `gitlab_list_pipelines`.
- Every method **and every parameter** must have `[Description("...")]`.
- Always delegate to the client and serialize via `ToolHelper.Serialize(result)`.
- Never put business logic in tool methods — keep them thin wrappers.

---

## Writing unit tests

### Library tests (`tests/Garrard.GitLab.Tests/`)

```csharp
public class PipelineClientTests
{
    private const string Domain = "gitlab.example.com";
    private const string Pat = "test-pat-token";

    private static PipelineClient CreateClient(Mock<IGitLabHttpClientFactory> factory, string domain = Domain) =>
        new PipelineClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = domain }));

    [Fact]
    public async Task GetPipelines_ReturnsSuccess()
    {
        var json = JsonSerializer.Serialize(new[] { new { id = 1, status = "success" } });
        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.OK, json);
        var client = CreateClient(factory);

        var result = await client.GetPipelines("my-project");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }
}
```

Key rules:
- Use `HttpTestHelpers.CreateMockFactory(statusCode, json)` for all HTTP mock setup.
- Use `HttpTestHelpers.CreateRealClientFactory()` only for invalid-domain / network-failure tests.
- Use `Options.Create(new GitLabOptions { ... })` — never mock `IOptions<GitLabOptions>`.
- Test both success and failure paths for every public method.
- Framework: **xUnit v3** (package `xunit.v3` 1.1.0) + **Moq**.

### MCP server tests (`tests/Garrard.GitLab.McpServer.Tests/`)

- Use `HostBuilder + UseTestServer()` (not `WebApplicationFactory<Program>`) for middleware tests.
- For tool tests, instantiate the tool class directly with a mock client.

---

## Verification steps

After making changes, always run:

```bash
dotnet build
dotnet test tests/Garrard.GitLab.Tests/
dotnet test tests/Garrard.GitLab.McpServer.Tests/
```

All 54+ tests must pass. Fix any errors before presenting results.
