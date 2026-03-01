using System.Text.Json;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>Shared serialisation helpers used by all MCP tool classes.</summary>
internal static class ToolHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Serializes a successful result to JSON, or returns an error string.</summary>
    internal static string Serialize<T>(Result<T> result) =>
        result.IsSuccess
            ? JsonSerializer.Serialize(result.Value, JsonOptions)
            : $"Error: {result.Error}";

    /// <summary>Returns "Success" or an error string for a non-generic Result.</summary>
    internal static string Serialize(Result result) =>
        result.IsSuccess ? "Success" : $"Error: {result.Error}";
}
