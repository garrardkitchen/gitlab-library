using Garrard.GitLab.McpServer.Tools;
using Microsoft.Extensions.Configuration;

namespace Garrard.GitLab.McpServer.Tests;

/// <summary>
/// Unit tests for the <see cref="ToolHelper"/> utility class.
/// </summary>
public class ToolHelperTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    [Fact]
    public void Resolve_WithExplicitValue_ReturnsExplicitValue()
    {
        var config = BuildConfig(new() { ["GL_PAT"] = "env-token" });
        var result = ToolHelper.Resolve(config, "explicit-token", "GL_PAT", "pat");
        Assert.Equal("explicit-token", result);
    }

    [Fact]
    public void Resolve_WithNullValueAndEnvKey_ReturnsEnvValue()
    {
        var config = BuildConfig(new() { ["GL_PAT"] = "env-token" });
        var result = ToolHelper.Resolve(config, null, "GL_PAT", "pat");
        Assert.Equal("env-token", result);
    }

    [Fact]
    public void Resolve_WithNullValueAndMissingEnvKey_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var ex = Assert.Throws<InvalidOperationException>(
            () => ToolHelper.Resolve(config, null, "GL_PAT", "pat"));
        Assert.Contains("GL_PAT", ex.Message);
        Assert.Contains("pat", ex.Message);
    }

    [Fact]
    public void Resolve_WithEmptyExplicitValue_ThrowsInvalidOperationException()
    {
        // Empty string is treated the same as missing — it doesn't satisfy the requirement.
        var config = BuildConfig(new() { ["GL_DOMAIN"] = "gitlab.com" });
        var ex = Assert.Throws<InvalidOperationException>(
            () => ToolHelper.Resolve(config, "", "GL_DOMAIN", "gitlabDomain"));
        Assert.Contains("GL_DOMAIN", ex.Message);
    }

    [Fact]
    public void Serialize_SuccessfulResult_ReturnsJson()
    {
        var result = CSharpFunctionalExtensions.Result.Success(new { id = 1, name = "test" });
        var json = ToolHelper.Serialize(result);
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"name\"", json);
    }

    [Fact]
    public void Serialize_FailedResult_ReturnsErrorString()
    {
        var result = CSharpFunctionalExtensions.Result.Failure<object>("Something went wrong");
        var json = ToolHelper.Serialize(result);
        Assert.StartsWith("Error:", json);
        Assert.Contains("Something went wrong", json);
    }

    [Fact]
    public void Serialize_UnitSuccessResult_ReturnsSuccess()
    {
        var result = CSharpFunctionalExtensions.Result.Success();
        Assert.Equal("Success", ToolHelper.Serialize(result));
    }

    [Fact]
    public void Serialize_UnitFailureResult_ReturnsErrorString()
    {
        var result = CSharpFunctionalExtensions.Result.Failure("API error");
        var text = ToolHelper.Serialize(result);
        Assert.StartsWith("Error:", text);
    }
}
