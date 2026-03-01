using System.Net;
using Garrard.GitLab.Library;
using Microsoft.Extensions.Options;

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
}
