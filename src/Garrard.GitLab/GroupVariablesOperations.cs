using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab;

public class GitLabGroupVariableDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonPropertyName("value")]
    public string Value { get; set; }
    
    [JsonPropertyName("variable_type")]
    public string VariableType { get; set; }
    
    [JsonPropertyName("protected")]
    public bool Protected { get; set; }
    
    [JsonPropertyName("masked")]
    public bool Masked { get; set; }
    
    [JsonPropertyName("environment_scope")]
    public string EnvironmentScope { get; set; }
}

public class GroupVariablesOperations
{
    /// <summary>
    /// Gets a variable from a GitLab group
    /// </summary>
    /// <param name="groupId">The ID of the group</param>
    /// <param name="variableKey">The key of the variable to get</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <returns>A Result containing the variable details if successful, or an error message if not</returns>
    public static async Task<Result<GitLabGroupVariableDto>> GetGroupVariable(string groupId, string variableKey, string pat, string gitlabDomain)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            var response = await client.GetAsync($"https://{gitlabDomain}/api/v4/groups/{groupId}/variables/{variableKey}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabGroupVariableDto>(responseBody);
                
                if (variable == null)
                {
                    return Result.Failure<GitLabGroupVariableDto>("Failed to deserialize variable data");
                }
                
                return Result.Success(variable);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return Result.Failure<GitLabGroupVariableDto>($"Failed to get variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroupVariableDto>($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Creates a new variable in a GitLab group or updates it if it already exists
    /// </summary>
    /// <param name="groupId">The ID of the group</param>
    /// <param name="variableKey">The key of the variable</param>
    /// <param name="variableValue">The value of the variable</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="variableType">Type of the variable (env_var or file, defaults to env_var)</param>
    /// <param name="isProtected">Whether the variable is protected</param>
    /// <param name="isMasked">Whether the variable is masked</param>
    /// <param name="environmentScope">The environment scope of the variable (defaults to *)</param>
    /// <returns>A Result containing the created/updated variable if successful, or an error message if not</returns>
    public static async Task<Result<GitLabGroupVariableDto>> CreateOrUpdateGroupVariable(
        string groupId, 
        string variableKey, 
        string variableValue, 
        string pat, 
        string gitlabDomain, 
        string variableType = "env_var", 
        bool isProtected = false, 
        bool isMasked = false, 
        string? environmentScope = "*", 
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            // First, check if the variable exists
            var getResult = await GetGroupVariable(groupId, variableKey, pat, gitlabDomain);
            bool variableExists = !getResult.IsFailure;
            
            string url = $"https://{gitlabDomain}/api/v4/groups/{groupId}/variables";
            HttpResponseMessage response;
            
            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", variableKey),
                new KeyValuePair<string, string>("value", variableValue),
                new KeyValuePair<string, string>("variable_type", variableType),
                new KeyValuePair<string, string>("protected", isProtected.ToString().ToLower()),
                // Masked doesn't work either
                // new KeyValuePair<string, string>("masked_and_hidden", isMasked.ToString().ToLower())
            };

            if (environmentScope != null)
            {
                fields.Add(
                    new KeyValuePair<string, string>("environment_scope", environmentScope));
            }

            var content = new FormUrlEncodedContent(fields);
            
            if (variableExists)
            {
                // Update the existing variable
                if (onMessage != null)
                {
                    onMessage("Variable exists, updating...");
                }
                response = await client.PutAsync($"{url}/{variableKey}", content);
            }
            else
            {
                // Create a new variable
                if (onMessage != null)
                {
                    onMessage("Variable does not exist, updating...");
                }
                response = await client.PostAsync(url, content);
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabGroupVariableDto>(responseBody);
                
                if (variable == null)
                {
                    return Result.Failure<GitLabGroupVariableDto>("Failed to deserialize variable data");
                }
                
                return Result.Success(variable);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                string action = variableExists ? "update" : "create";
                return Result.Failure<GitLabGroupVariableDto>($"Failed to {action} variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroupVariableDto>($"An error occurred: {ex.Message}");
        }
    }
}