using Garrard.GitLab.Library;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="GroupVariableClient"/>.
/// </summary>
public class GroupVariablesOperationsTests
{
    private const string Pat = "test-pat-token";
    private const string InvalidDomain = "not-a-real-domain.invalid";

    private static GroupVariableClient CreateInvalidDomainClient() =>
        new GroupVariableClient(
            HttpTestHelpers.CreateRealClientFactory().Object,
            Options.Create(new GitLabOptions { Pat = Pat, Domain = InvalidDomain }));

    [Fact]
    public async Task GetGroupVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().GetGroupVariable("123", "MY_VAR");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateOrUpdateGroupVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await CreateInvalidDomainClient().CreateOrUpdateGroupVariable(
            "123", "KEY", "value");
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("env_var")]
    [InlineData("file")]
    public async Task CreateOrUpdateGroupVariable_ValidVariableTypes_DoNotThrow(string variableType)
    {
        var exception = await Record.ExceptionAsync(() =>
            CreateInvalidDomainClient().CreateOrUpdateGroupVariable(
                "123", "KEY", "value",
                variableType: variableType));
        Assert.Null(exception);
    }
}
