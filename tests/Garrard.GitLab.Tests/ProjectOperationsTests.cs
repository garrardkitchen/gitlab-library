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
            masked = false,
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
    }
}
