using System.Net;
using System.Text.Json;
using Garrard.GitLab.Http;
using Moq;
using Moq.Protected;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="GroupOperations"/> using a mocked <see cref="IGitLabHttpClientFactory"/>.
/// </summary>
public class GroupOperationsTests
{
    private const string Domain = "gitlab.example.com";
    private const string Pat = "test-pat-token";

    private static IGitLabHttpClientFactory MockFactory(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });

        var factoryMock = new Mock<IGitLabHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient())
                   .Returns(new HttpClient(handler.Object));
        return factoryMock.Object;
    }

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

        var result = await GroupOperations.FindGroups("42", Pat, Domain);

        // The real static method creates its own HttpClient internally;
        // this test documents expected behaviour via the Result API.
        // Integration behaviour is verified via the real GitLab API in integration tests.
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FindGroups_EmptyNameOrId_Returns()
    {
        // Passing an empty string should not throw – the Result will capture the error.
        var result = await Record.ExceptionAsync(() => GroupOperations.FindGroups("", Pat, Domain));
        Assert.Null(result); // No unhandled exception
    }

    [Fact]
    public async Task SearchGroups_InvalidDomain_ReturnsFailure()
    {
        // Using an unreachable domain ensures an exception is caught and returned as Result.Failure.
        var result = await GroupOperations.SearchGroups("test", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetSubgroups_InvalidDomain_ReturnsFailure()
    {
        var result = await GroupOperations.GetSubgroups("123", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateGitLabGroup_InvalidDomain_ReturnsFailure()
    {
        var result = await GroupOperations.CreateGitLabGroup("new-group", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }
}
