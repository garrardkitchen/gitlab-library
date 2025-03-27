using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab;

/// <summary>
/// Represents a GitLab project
/// </summary>
public class GitLabProjectInfoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; }
    
    [JsonPropertyName("ssh_url_to_repo")]
    public string SshUrlToRepo { get; set; }
    
    [JsonPropertyName("http_url_to_repo")]
    public string HttpUrlToRepo { get; set; }
    
    [JsonPropertyName("path")]
    public string Path { get; set; }
    
    [JsonPropertyName("namespace")]
    public GitLabNamespaceDto Namespace { get; set; }
    
    /// <summary>
    /// Gets the ID of the group that the project belongs to
    /// </summary>
    public int GroupId => Namespace?.Id ?? 0;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("last_activity_at")]
    public DateTime LastActivityAt { get; set; }
    
    [JsonPropertyName("marked_for_deletion_at")]
    public string? MarkedForDeletionAt { get; set; }
    
    /// <summary>
    /// Indicates whether the project is marked for deletion
    /// </summary>
    public bool IsMarkedForDeletion => !string.IsNullOrEmpty(MarkedForDeletionAt);
}

/// <summary>
/// Represents a GitLab namespace
/// </summary>
public class GitLabNamespaceDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("path")]
    public string Path { get; set; }
    
    [JsonPropertyName("kind")]
    public string Kind { get; set; }
    
    [JsonPropertyName("full_path")]
    public string FullPath { get; set; }
}

/// <summary>
/// Represents a GitLab project variable
/// </summary>
public class GitLabProjectVariableDto
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

/// <summary>
/// Operations for working with GitLab projects
/// </summary>
public class ProjectOperations
{
    /// <summary>
    /// Gets all projects within a GitLab group
    /// </summary>
    /// <param name="groupIdOrName">The ID or name of the group</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="includeSubgroups">Whether to include projects from subgroups</param>
    /// <param name="orderBy">Field to order by (id, name, path, created_at, updated_at, last_activity_at)</param>
    /// <param name="sort">Sort direction (asc or desc)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing an array of GitLab projects if successful, or an error message if not</returns>
    public static async Task<Result<GitLabProjectInfoDto[]>> GetProjectsInGroup(
        string groupIdOrName, 
        string pat, 
        string gitlabDomain,
        bool includeSubgroups = true,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Retrieving projects for group {groupIdOrName}...");
            
            // First, resolve the ID if a name was provided
            string groupId = groupIdOrName;
            if (!int.TryParse(groupIdOrName, out _))
            {
                var groupResponse = await client.GetAsync($"https://{gitlabDomain}/api/v4/groups?search={groupIdOrName}");
                if (!groupResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await groupResponse.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabProjectInfoDto[]>($"Failed to find group by name: {groupResponse.StatusCode}. {errorResponse}");
                }
                
                var groups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(
                    await groupResponse.Content.ReadAsStringAsync());
                
                if (groups == null || !groups.Any(g => !g.IsMarkedForDeletion))
                {
                    return Result.Failure<GitLabProjectInfoDto[]>($"No active group found with name: {groupIdOrName}");
                }
                
                // Use the first active match (most relevant)
                var activeGroup = groups.FirstOrDefault(g => !g.IsMarkedForDeletion);
                if (activeGroup != null)
                {
                    groupId = activeGroup.Id.ToString();
                    onMessage?.Invoke($"Resolved group name to ID: {groupId}");
                }
                else
                {
                    return Result.Failure<GitLabProjectInfoDto[]>($"No active group found with name: {groupIdOrName}");
                }
            }
            
            // Validate orderBy parameter
            var validOrderByFields = new[] { "id", "name", "path", "created_at", "updated_at", "last_activity_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
            {
                orderBy = "name"; // Default to name if invalid
            }
            
            // Validate sort parameter
            sort = sort.ToLower() == "desc" ? "desc" : "asc";
            
            // Set up pagination and fetch all pages
            var allProjects = new List<GitLabProjectInfoDto>();
            int page = 1;
            const int perPage = 100; // Maximum allowed by GitLab API
            bool hasMorePages = true;
            
            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of projects...");
                
                // Get projects with paging
                var url = $"https://{gitlabDomain}/api/v4/groups/{groupId}/projects" +
                         $"?per_page={perPage}" +
                         $"&page={page}" +
                         $"&order_by={orderBy}" +
                         $"&sort={sort}" +
                         $"&include_subgroups={includeSubgroups.ToString().ToLower()}";
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var projects = JsonSerializer.Deserialize<List<GitLabProjectInfoDto>>(responseBody);
                    
                    if (projects == null)
                    {
                        return Result.Failure<GitLabProjectInfoDto[]>("Failed to deserialize projects data");
                    }
                    
                    // Add current page results to our collection, filtering out projects marked for deletion
                    allProjects.AddRange(projects.Where(p => !p.IsMarkedForDeletion));
                    
                    // Check if there are more pages
                    if (projects.Count < perPage)
                    {
                        hasMorePages = false;
                    }
                    else
                    {
                        // Try to get total pages from header
                        if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesHeader))
                        {
                            if (int.TryParse(totalPagesHeader.FirstOrDefault(), out int totalPages))
                            {
                                hasMorePages = page < totalPages;
                            }
                        }
                        page++;
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabProjectInfoDto[]>($"Failed to get projects: {response.StatusCode}. {errorResponse}");
                }
            }
            
            onMessage?.Invoke($"Retrieved a total of {allProjects.Count} active projects from group {groupIdOrName}");
            return Result.Success(allProjects.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabProjectInfoDto[]>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all variables for a GitLab project
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing an array of project variables if successful, or an error message if not</returns>
    public static async Task<Result<GitLabProjectVariableDto[]>> GetProjectVariables(
        string projectId, 
        string pat, 
        string gitlabDomain,
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Retrieving variables for project {projectId}...");
            
            // Set up pagination and fetch all pages
            var allVariables = new List<GitLabProjectVariableDto>();
            int page = 1;
            const int perPage = 100; // Maximum allowed by GitLab API
            bool hasMorePages = true;
            
            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of project variables...");
                
                var url = $"https://{gitlabDomain}/api/v4/projects/{projectId}/variables" +
                         $"?per_page={perPage}" +
                         $"&page={page}";
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var variables = JsonSerializer.Deserialize<List<GitLabProjectVariableDto>>(responseBody);
                    
                    if (variables == null)
                    {
                        return Result.Failure<GitLabProjectVariableDto[]>("Failed to deserialize variables data");
                    }
                    
                    // Add current page results to our collection
                    allVariables.AddRange(variables);
                    
                    // Check if there are more pages
                    if (variables.Count < perPage)
                    {
                        hasMorePages = false;
                    }
                    else
                    {
                        // Try to get total pages from header
                        if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesHeader))
                        {
                            if (int.TryParse(totalPagesHeader.FirstOrDefault(), out int totalPages))
                            {
                                hasMorePages = page < totalPages;
                            }
                        }
                        page++;
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabProjectVariableDto[]>($"Failed to get project variables: {response.StatusCode}. {errorResponse}");
                }
            }
            
            onMessage?.Invoke($"Retrieved a total of {allVariables.Count} variables from project {projectId}");
            return Result.Success(allVariables.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabProjectVariableDto[]>($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Creates or updates a variable in a GitLab project
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="variableKey">The key of the variable</param>
    /// <param name="variableValue">The value of the variable</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="variableType">Type of the variable (env_var or file, defaults to env_var)</param>
    /// <param name="isProtected">Whether the variable is protected</param>
    /// <param name="isMasked">Whether the variable is masked</param>
    /// <param name="environmentScope">The environment scope of the variable (defaults to *)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing the created/updated variable if successful, or an error message if not</returns>
    public static async Task<Result<GitLabProjectVariableDto>> CreateOrUpdateProjectVariable(
        string projectId, 
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
            onMessage?.Invoke($"Checking if variable {variableKey} exists in project {projectId}...");
            
            var getResult = await GetProjectVariable(projectId, variableKey, pat, gitlabDomain, environmentScope);
            bool variableExists = !getResult.IsFailure;
            
            string url = $"https://{gitlabDomain}/api/v4/projects/{projectId}/variables";
            HttpResponseMessage response;
            
            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key", variableKey),
                new KeyValuePair<string, string>("value", variableValue),
                new KeyValuePair<string, string>("variable_type", variableType),
                new KeyValuePair<string, string>("protected", isProtected.ToString().ToLower()),
                new KeyValuePair<string, string>("masked", isMasked.ToString().ToLower())
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
                onMessage?.Invoke("Variable exists, updating...");
                response = await client.PutAsync($"{url}/{variableKey}", content);
            }
            else
            {
                // Create a new variable
                onMessage?.Invoke("Variable does not exist, creating...");
                response = await client.PostAsync(url, content);
            }
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabProjectVariableDto>(responseBody);
                
                if (variable == null)
                {
                    return Result.Failure<GitLabProjectVariableDto>("Failed to deserialize variable data");
                }
                
                onMessage?.Invoke($"Successfully {(variableExists ? "updated" : "created")} variable {variableKey}");
                return Result.Success(variable);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                string action = variableExists ? "update" : "create";
                return Result.Failure<GitLabProjectVariableDto>($"Failed to {action} variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabProjectVariableDto>($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets a specific variable from a GitLab project
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="variableKey">The key of the variable to get</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="environmentScope">The environment scope of the variable (defaults to *)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing the variable if successful, or an error message if not</returns>
    public static async Task<Result<GitLabProjectVariableDto>> GetProjectVariable(
        string projectId, 
        string variableKey, 
        string pat, 
        string gitlabDomain,
        string? environmentScope = "*",
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Retrieving variable {variableKey} for project {projectId}...");
            
            string url = $"https://{gitlabDomain}/api/v4/projects/{projectId}/variables/{variableKey}";
            if (environmentScope != null && environmentScope != "*")
            {
                url += $"?filter[environment_scope]={Uri.EscapeDataString(environmentScope)}";
            }
            
            var response = await client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabProjectVariableDto>(responseBody);
                
                if (variable == null)
                {
                    return Result.Failure<GitLabProjectVariableDto>("Failed to deserialize variable data");
                }
                
                onMessage?.Invoke($"Successfully retrieved variable {variableKey}");
                return Result.Success(variable);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return Result.Failure<GitLabProjectVariableDto>($"Failed to get variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabProjectVariableDto>($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Deletes a variable from a GitLab project
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="variableKey">The key of the variable to delete</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="environmentScope">The environment scope of the variable (defaults to *)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result indicating success or failure</returns>
    public static async Task<Result> DeleteProjectVariable(
        string projectId, 
        string variableKey, 
        string pat, 
        string gitlabDomain,
        string? environmentScope = "*",
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Deleting variable {variableKey} from project {projectId}...");
            
            string url = $"https://{gitlabDomain}/api/v4/projects/{projectId}/variables/{variableKey}";
            if (environmentScope != null && environmentScope != "*")
            {
                url += $"?filter[environment_scope]={Uri.EscapeDataString(environmentScope)}";
            }
            
            var response = await client.DeleteAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                onMessage?.Invoke($"Successfully deleted variable {variableKey}");
                return Result.Success();
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return Result.Failure($"Failed to delete variable: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"An error occurred: {ex.Message}");
        }
    }
}