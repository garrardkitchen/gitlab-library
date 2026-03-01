using CSharpFunctionalExtensions;
using Garrard.GitLab.McpServer.Tools;

namespace Garrard.GitLab.McpServer.Tests;

/// <summary>
/// Unit tests for <see cref="ToolHelper"/> serialisation methods.
/// </summary>
public class ToolHelperTests
{
    // --- Serialize<T> (Result<T>) ---

    [Fact]
    public void Serialize_SuccessfulResult_ReturnsJson()
    {
        var result = Result.Success(new { name = "test", value = 42 });

        var json = ToolHelper.Serialize(result);

        Assert.Contains("\"name\"", json);
        Assert.Contains("\"test\"", json);
        Assert.Contains("\"value\"", json);
        Assert.Contains("42", json);
    }

    [Fact]
    public void Serialize_FailedResult_ReturnsErrorString()
    {
        var result = Result.Failure<string>("something went wrong");

        var output = ToolHelper.Serialize(result);

        Assert.Equal("Error: something went wrong", output);
    }

    // --- Serialize (Result) ---

    [Fact]
    public void Serialize_UnitSuccessResult_ReturnsSuccessString()
    {
        var result = Result.Success();

        var output = ToolHelper.Serialize(result);

        Assert.Equal("Success", output);
    }

    [Fact]
    public void Serialize_UnitFailureResult_ReturnsErrorString()
    {
        var result = Result.Failure("unit failure");

        var output = ToolHelper.Serialize(result);

        Assert.Equal("Error: unit failure", output);
    }
}
