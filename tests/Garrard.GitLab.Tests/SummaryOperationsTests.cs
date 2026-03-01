namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="SummaryOperations"/>.
/// </summary>
public class SummaryOperationsTests
{
    private const string Pat = "test-pat-token";

    [Fact]
    public async Task GetGroupSummary_InvalidDomain_ReturnsFailure()
    {
        var result = await SummaryOperations.GetGroupSummary("123", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjectSummary_InvalidDomain_ReturnsSuccessWithZeroVariables()
    {
        // GetProjectSummary always returns Success even when sub-operations fail;
        // sub-operation failures result in variableCount = 0 rather than a hard failure.
        var result = await SummaryOperations.GetProjectSummary(123, Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.VariableCount);
    }

    [Fact]
    public async Task GetGroupProjectsSummary_InvalidDomain_ReturnsFailure()
    {
        var result = await SummaryOperations.GetGroupProjectsSummary("my-group", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }
}
