using Garrard.GitLab.Library;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="SummaryClient"/>.
/// </summary>
public class SummaryOperationsTests
{
    private const string Pat = "test-pat-token";
    private const string InvalidDomain = "not-a-real-domain.invalid";

    private static GroupClient CreateInvalidDomainGroupClient() =>
        new GroupClient(
            HttpTestHelpers.CreateRealClientFactory().Object,
            Options.Create(new GitLabOptions { Pat = Pat, Domain = InvalidDomain }));

    private static ProjectClient CreateInvalidDomainProjectClient() =>
        new ProjectClient(
            HttpTestHelpers.CreateRealClientFactory().Object,
            Options.Create(new GitLabOptions { Pat = Pat, Domain = InvalidDomain }));

    [Fact]
    public async Task GetGroupSummary_InvalidDomain_ReturnsFailure()
    {
        var summaryClient = new SummaryClient(
            CreateInvalidDomainGroupClient(),
            CreateInvalidDomainProjectClient());

        var result = await summaryClient.GetGroupSummary("123");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjectSummary_InvalidDomain_ReturnsSuccessWithZeroVariables()
    {
        // GetProjectSummary always returns Success even when sub-operations fail;
        // sub-operation failures result in variableCount = 0 rather than a hard failure.
        var summaryClient = new SummaryClient(
            CreateInvalidDomainGroupClient(),
            CreateInvalidDomainProjectClient());

        var result = await summaryClient.GetProjectSummary(123);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.VariableCount);
    }

    [Fact]
    public async Task GetGroupProjectsSummary_InvalidDomain_ReturnsFailure()
    {
        var summaryClient = new SummaryClient(
            CreateInvalidDomainGroupClient(),
            CreateInvalidDomainProjectClient());

        var result = await summaryClient.GetGroupProjectsSummary("my-group");
        Assert.True(result.IsFailure);
    }
}
