using System.Net;
using System.Text.Json;
using Garrard.GitLab.Library;
using Garrard.GitLab.Library.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="GroupClient"/> using a mocked <see cref="IGitLabHttpClientFactory"/>.
/// </summary>
public class GroupOperationsTests
{
    private const string Domain = "gitlab.example.com";
    private const string Pat = "test-pat-token";
    private const string InvalidDomain = "not-a-real-domain.invalid";

    private static GroupClient CreateClient(Mock<IGitLabHttpClientFactory> factory, string domain = Domain) =>
        new GroupClient(factory.Object, Options.Create(new GitLabOptions { Pat = Pat, Domain = domain }));

    private static GroupClient CreateInvalidDomainClient() =>
        CreateClient(HttpTestHelpers.CreateRealClientFactory(), InvalidDomain);

    [Fact]
    public async Task FindGroups_ById_ReturnsGroup()
    {
        var groupJson = JsonSerializer.Serialize(new
        {
            id = 42,
            name = "my-group",
            path = "my-group",
            full_path = "my-group",
            web_url = $"https://{Domain}/groups/my-group",
            parent_id = (int?)null,
            has_subgroups = false,
            marked_for_deletion_on = (string?)null
        });

        var factory = HttpTestHelpers.CreateMockFactory(HttpStatusCode.OK, groupJson);
        var client = CreateClient(factory);

        var result = await client.FindGroups("42");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(42, result.Value[0].Id);
    }

    [Fact]
    public async Task FindGroups_EmptyNameOrId_Returns()
    {
        // Passing an empty string should not throw – the Result will capture the error.
        var client = CreateInvalidDomainClient();
        var exception = await Record.ExceptionAsync(() => client.FindGroups(""));
        Assert.Null(exception); // No unhandled exception
    }

    [Fact]
    public async Task SearchGroups_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().SearchGroups("test");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetSubgroups_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().GetSubgroups("123");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateGitLabGroup_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().CreateGitLabGroup("new-group");
        Assert.True(result.IsFailure);
    }
}
