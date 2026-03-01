using System.Text.Json;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab.McpServer.Tools;

/// <summary>Shared helper utilities used by all MCP tool classes.</summary>
internal static class ToolHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Resolves a value by preferring the explicit <paramref name="value"/> parameter,
    /// then falling back to a configuration key. Throws if neither is available.
    /// </summary>
    internal static string Resolve(IConfiguration config, string? value, string envKey, string paramName)
    {
        var resolved = value ?? config[envKey];
        if (string.IsNullOrWhiteSpace(resolved))
            throw new InvalidOperationException(
                $"'{paramName}' was not provided and '{envKey}' is not set. " +
                $"Set the environment variable or pass the parameter explicitly.");
        return resolved;
    }

    /// <summary>Serializes a successful result or returns an error string.</summary>
    internal static string Serialize<T>(Result<T> result) =>
        result.IsSuccess
            ? JsonSerializer.Serialize(result.Value, JsonOptions)
            : $"Error: {result.Error}";

    /// <summary>Returns "Success" or an error string for a non-generic Result.</summary>
    internal static string Serialize(Result result) =>
        result.IsSuccess ? "Success" : $"Error: {result.Error}";
}
