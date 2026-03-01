namespace Garrard.GitLab.Tests;

/// <summary>
/// Unit tests for <see cref="GroupVariablesOperations"/>.
/// </summary>
public class GroupVariablesOperationsTests
{
    private const string Pat = "test-pat-token";

    [Fact]
    public async Task GetGroupVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await GroupVariablesOperations.GetGroupVariable("123", "MY_VAR", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateOrUpdateGroupVariable_InvalidDomain_ReturnsFailure()
    {
        var result = await GroupVariablesOperations.CreateOrUpdateGroupVariable(
            "123", "KEY", "value", Pat, "not-a-real-domain.invalid");
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("env_var")]
    [InlineData("file")]
    public async Task CreateOrUpdateGroupVariable_ValidVariableTypes_DoNotThrow(string variableType)
    {
        var exception = await Record.ExceptionAsync(() =>
            GroupVariablesOperations.CreateOrUpdateGroupVariable(
                "123", "KEY", "value", Pat, "not-a-real-domain.invalid",
                variableType: variableType));
        Assert.Null(exception);
    }
}
