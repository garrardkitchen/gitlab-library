using System.Text.Json;
using CSharpFunctionalExtensions;
using Garrard.GitLab.Library.DTOs;
using Garrard.GitLab.Library.Http;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Library;

/// <summary>
/// Instance client for working with GitLab group variables.
/// </summary>
public sealed class GroupVariableClient
{
    private readonly IGitLabHttpClientFactory _factory;
    private readonly GitLabOptions _opts;

    public GroupVariableClient(IGitLabHttpClientFactory factory, IOptions<GitLabOptions> options)
    {
        _factory = factory;
        _opts = options.Value;
    }

    /// <summary>
    /// Gets a variable from a GitLab group.
    /// </summary>
    public async Task<Result<GitLabVariable>> GetGroupVariable(string groupId, string variableKey)
    {
        var client = _factory.CreateClient();

        try
        {
            var response = await client.GetAsync($"https://{_opts.Domain}/api/v4/groups/{groupId}/variables/{variableKey}");

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabVariable>(responseBody);

                if (variable == null)
                    return Result.Failure<GitLabVariable>("Failed to deserialize variable data");

                return Result.Success(variable);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return Result.Failure<GitLabVariable>($"Failed to get variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabVariable>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new variable in a GitLab group or updates it if it already exists.
    /// </summary>
    public async Task<Result<GitLabVariable>> CreateOrUpdateGroupVariable(
        string groupId,
        string variableKey,
        string variableValue,
        string variableType = "env_var",
        bool isProtected = false,
        bool isMasked = false,
        string? environmentScope = "*",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            var getResult = await GetGroupVariable(groupId, variableKey);
            bool variableExists = !getResult.IsFailure;

            string url = $"https://{_opts.Domain}/api/v4/groups/{groupId}/variables";
            HttpResponseMessage response;

            var fields = new List<KeyValuePair<string, string>>
            {
                new("key", variableKey),
                new("value", variableValue),
                new("variable_type", variableType),
                new("protected", isProtected.ToString().ToLower()),
                new("masked", isMasked.ToString().ToLower())
            };

            if (environmentScope != null)
                fields.Add(new("environment_scope", environmentScope));

            var content = new FormUrlEncodedContent(fields);

            if (variableExists)
            {
                onMessage?.Invoke("Variable exists, updating...");
                response = await client.PutAsync($"{url}/{variableKey}", content);
            }
            else
            {
                onMessage?.Invoke("Variable does not exist, updating...");
                response = await client.PostAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabVariable>(responseBody);

                if (variable == null)
                    return Result.Failure<GitLabVariable>("Failed to deserialize variable data");

                return Result.Success(variable);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                string action = variableExists ? "update" : "create";
                return Result.Failure<GitLabVariable>($"Failed to {action} variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabVariable>($"An error occurred: {ex.Message}");
        }
    }
}
