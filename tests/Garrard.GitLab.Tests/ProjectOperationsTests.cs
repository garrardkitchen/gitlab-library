using System.Net;
using System.Text.Json;
using Garrard.GitLab.Library;
using Garrard.GitLab.Library.DTOs;
using Garrard.GitLab.Library.Enums;
using Garrard.GitLab.Library.Http;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="ProjectClient"/>.
/// </summary>
public class ProjectOperationsTests
{
    private const string Domain = "gitlab.example.com";
    private const string Pat = "test-pat-token";
    private const string InvalidDomain = "not-a-real-domain.invalid";

    private static ProjectClient CreateInvalidDomainClient() =>
        new ProjectClient(
            HttpTestHelpers.CreateRealClientFactory().Object,
            Options.Create(new GitLabOptions { Pat = Pat, Domain = InvalidDomain }));

    [Fact]
    public async Task GetProjectsInGroup_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().GetProjectsInGroup("123");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjectVariables_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().GetProjectVariables("123");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjectVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().GetProjectVariable("123", "MY_VAR");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteProjectVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().DeleteProjectVariable("123", "MY_VAR");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateOrUpdateProjectVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().CreateOrUpdateProjectVariable(
            "123", "KEY", "value");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateGitLabProject_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().CreateGitLabProject("my-project");
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("name")]
    [InlineData("path")]
    [InlineData("created_at")]
    [InlineData("updated_at")]
    [InlineData("last_activity_at")]
    public async Task GetProjectsInGroup_ValidOrderByValues_DoNotThrow(string orderBy)
    {
        // Validates that all documented orderBy values are accepted without exceptions.
        var exception = await Record.ExceptionAsync(() =>
            CreateInvalidDomainClient().GetProjectsInGroup("123", orderBy: orderBy));
        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateProjectAccessToken_ReturnsSuccess()
    {
        var tokenJson = JsonSerializer.Serialize(new
        {
            id = 1,
            name = "GL_TOKEN",
            token = "glpat-secret123",
            scopes = new[] { "write_repository" },
            access_level = 40,
            expires_at = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
            created_at = DateTime.UtcNow.ToString("o"),
            revoked = false,
            active = true
        });

        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.Created, tokenJson);
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.CreateProjectAccessToken("99");

        Assert.True(result.IsSuccess);
        Assert.Equal("GL_TOKEN", result.Value.Name);
        Assert.Equal("glpat-secret123", result.Value.Token);
        Assert.Equal(40, result.Value.AccessLevel);
        Assert.Contains("write_repository", result.Value.Scopes);
        Assert.True(result.Value.Active);
    }

    [Fact]
    public async Task CreateProjectAccessToken_ApiFailure_ReturnsFailure()
    {
        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.Forbidden, "{\"message\":\"403 Forbidden\"}");
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.CreateProjectAccessToken("99");

        Assert.True(result.IsFailure);
        Assert.Contains("403", result.Error);
    }

    [Fact]
    public async Task CreateProjectAccessToken_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().CreateProjectAccessToken("99");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateProjectAccessToken_CustomScopesAndLevel_ReturnsSuccess()
    {
        var tokenJson = JsonSerializer.Serialize(new
        {
            id = 2,
            name = "MY_TOKEN",
            token = "glpat-abc",
            scopes = new[] { "read_api", "read_repository" },
            access_level = 30,
            expires_at = "2027-01-01",
            created_at = DateTime.UtcNow.ToString("o"),
            revoked = false,
            active = true
        });

        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.Created, tokenJson);
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.CreateProjectAccessToken(
            "99", "MY_TOKEN",
            ProjectAccessTokenScope.ReadApi | ProjectAccessTokenScope.ReadRepository,
            AccessLevel.Developer,
            new DateOnly(2027, 1, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.AccessLevel);
    }

    [Fact]
    public async Task CreateOrUpdateProjectVariable_SetsHiddenTrue_ReturnsSuccess()
    {
        var varJson = JsonSerializer.Serialize(new
        {
            key = "API_KEY",
            value = "secret",
            variable_type = "env_var",
            @protected = false,
            masked = true,   // GitLab always returns masked=true when hidden=true
            hidden = true,
            environment_scope = "*"
        });

        // GetProjectVariable returns 404 (not found) so a POST is issued
        var handler = new Moq.Mock<System.Net.Http.HttpMessageHandler>(Moq.MockBehavior.Loose);
        var callCount = 0;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)   // GET check
                    : new HttpResponseMessage(HttpStatusCode.OK)         // POST create
                      { Content = new StringContent(varJson, System.Text.Encoding.UTF8, "application/json") };
            });

        var factoryMock = new Moq.Mock<IGitLabHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient()).Returns(new HttpClient(handler.Object));

        var client = new ProjectClient(factoryMock.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.CreateOrUpdateProjectVariable("99", "API_KEY", "secret", isHidden: true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Hidden);
        Assert.True(result.Value.Masked);
    }

    [Fact]
    public async Task CreateOrUpdateProjectVariable_HiddenForcesIsMasked_SentInRequest()
    {
        string? capturedBody = null;

        var varJson = JsonSerializer.Serialize(new
        {
            key = "MY_VAR", value = "val", variable_type = "env_var",
            @protected = false, masked = true, hidden = true, environment_scope = "*"
        });

        var handler = new Moq.Mock<System.Net.Http.HttpMessageHandler>(Moq.MockBehavior.Loose);
        var callCount = 0;
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage req, CancellationToken _) =>
            {
                callCount++;
                if (callCount == 2)
                    capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                var resp = callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                      { Content = new StringContent(varJson, System.Text.Encoding.UTF8, "application/json") };
                return Task.FromResult(resp);
            });

        var factoryMock = new Moq.Mock<IGitLabHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient()).Returns(new HttpClient(handler.Object));

        var client = new ProjectClient(factoryMock.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        // Pass isHidden=true but isMasked=false — library should promote masked to true
        await client.CreateOrUpdateProjectVariable("99", "MY_VAR", "val", isMasked: false, isHidden: true);

        Assert.NotNull(capturedBody);
        Assert.Contains("masked=true", capturedBody);
        Assert.Contains("masked_and_hidden=true", capturedBody);
        // "hidden" must not appear as a standalone field (masked_and_hidden is fine)
        Assert.DoesNotContain("&hidden=", capturedBody);
    }

    // ── SearchProjects ──────────────────────────────────────────────────────

    [Fact]
    public async Task SearchProjects_NoCriteria_ReturnsFailure()
    {
        var client = new ProjectClient(
            HttpTestHelpers.CreateMockFactory(HttpStatusCode.OK, "[]").Object,
            Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.SearchProjects();

        Assert.True(result.IsFailure);
        Assert.Contains("search criterion", result.Error);
    }

    [Fact]
    public async Task SearchProjects_ById_ReturnsSuccess()
    {
        var projectJson = JsonSerializer.Serialize(new
        {
            id = 42,
            name = "my-project",
            description = "",
            web_url = "https://gitlab.example.com/group/my-project",
            ssh_url_to_repo = "",
            http_url_to_repo = "",
            path = "my-project",
            path_with_namespace = "group/my-project",
            @namespace = new { id = 5, name = "group", path = "group", kind = "group", full_path = "group" },
            created_at = DateTime.UtcNow.ToString("o"),
            last_activity_at = DateTime.UtcNow.ToString("o"),
            marked_for_deletion_at = (string?)null
        });

        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.OK, projectJson);
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.SearchProjects(id: 42);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(42, result.Value.Items[0].Id);
        Assert.Equal("my-project", result.Value.Items[0].Name);
        Assert.Equal(1, result.Value.TotalPages);
    }

    [Fact]
    public async Task SearchProjects_ById_NotFound_ReturnsFailure()
    {
        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.NotFound, "{\"message\":\"404 Not Found\"}");
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.SearchProjects(id: 9999);

        Assert.True(result.IsFailure);
        Assert.Contains("404", result.Error);
    }

    [Fact]
    public async Task SearchProjects_ByName_ReturnsPaged()
    {
        var projectsJson = JsonSerializer.Serialize(new[]
        {
            new { id = 1, name = "foo-api", description = "", web_url = "", ssh_url_to_repo = "",
                  http_url_to_repo = "", path = "foo-api", path_with_namespace = "ns/foo-api",
                  @namespace = new { id = 1, name = "ns", path = "ns", kind = "group", full_path = "ns" },
                  created_at = DateTime.UtcNow.ToString("o"), last_activity_at = DateTime.UtcNow.ToString("o"),
                  marked_for_deletion_at = (string?)null },
            new { id = 2, name = "foo-web", description = "", web_url = "", ssh_url_to_repo = "",
                  http_url_to_repo = "", path = "foo-web", path_with_namespace = "ns/foo-web",
                  @namespace = new { id = 1, name = "ns", path = "ns", kind = "group", full_path = "ns" },
                  created_at = DateTime.UtcNow.ToString("o"), last_activity_at = DateTime.UtcNow.ToString("o"),
                  marked_for_deletion_at = (string?)null }
        });

        var handler = new Moq.Mock<System.Net.Http.HttpMessageHandler>(Moq.MockBehavior.Loose);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(projectsJson, System.Text.Encoding.UTF8, "application/json"),
                Headers = { { "X-Total", "2" }, { "X-Total-Pages", "1" } }
            });

        var factoryMock = new Moq.Mock<IGitLabHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient()).Returns(new HttpClient(handler.Object));

        var client = new ProjectClient(factoryMock.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.SearchProjects(search: "foo", page: 1, perPage: 20);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Length);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(1, result.Value.Page);
    }

    [Fact]
    public async Task SearchProjects_ByName_EmptyResults_ReturnsEmptyPage()
    {
        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.OK, "[]");
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.SearchProjects(search: "nonexistent-xyz");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
    }

    [Fact]
    public async Task SearchProjects_ApiFailure_ReturnsFailure()
    {
        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.Unauthorized, "{\"message\":\"401 Unauthorized\"}");
        var client = new ProjectClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = Domain }));

        var result = await client.SearchProjects(search: "anything");

        Assert.True(result.IsFailure);
        Assert.Contains("401", result.Error);
    }

    [Fact]
    public async Task SearchProjects_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().SearchProjects(search: "test");
        Assert.True(result.IsFailure);
    }
}
