using System.Net;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="ProjectOperations"/>.
/// </summary>
public class ProjectOperationsTests
{
    private const string Domain = "gitlab.example.com";
    private const string Pat = "test-pat-token";

    [Fact]
    public async Task GetProjectsInGroup_InvalidDomain_ReturnsFailure()
    {
        var result = await ProjectOperations.GetProjectsInGroup("123", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjectVariables_InvalidDomain_ReturnsFailure()
    {
        var result = await ProjectOperations.GetProjectVariables("123", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjectVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await ProjectOperations.GetProjectVariable("123", "MY_VAR", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteProjectVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await ProjectOperations.DeleteProjectVariable("123", "MY_VAR", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateOrUpdateProjectVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await ProjectOperations.CreateOrUpdateProjectVariable(
            "123", "KEY", "value", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateGitLabProject_InvalidDomain_ReturnsFailure()
    {
        var result = await ProjectOperations.CreateGitLabProject("my-project", Pat, "not-a-real-domain.invalid");
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
            ProjectOperations.GetProjectsInGroup("123", Pat, "not-a-real-domain.invalid", orderBy: orderBy));
        Assert.Null(exception);
    }
}
