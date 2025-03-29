using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Garrard.GitLab;

/// <summary>
/// Represents a GitLab group
/// </summary>
public class GitLabGroupDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("path")]
    public string Path { get; set; }
    
    [JsonPropertyName("full_path")]
    public string FullPath { get; set; }
    
    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; }
    
    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }
    
    [JsonPropertyName("has_subgroups")]
    public bool HasSubgroups { get; set; }
    
    [JsonPropertyName("marked_for_deletion_on")]
    public string? MarkedForDeletionOn { get; set; }
    
    /// <summary>
    /// Indicates whether the group is marked for deletion
    /// </summary>
    public bool IsMarkedForDeletion => !string.IsNullOrEmpty(MarkedForDeletionOn);
}

/// <summary>
/// Operations for working with GitLab groups
/// </summary>
public class GroupOperations
{
    /// <summary>
    /// Gets all GitLab groups beneath a specified group
    /// </summary>
    /// <param name="groupIdOrName">The ID or name of the parent group</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="orderBy">Field to order by (id, name, path, or created_at)</param>
    /// <param name="sort">Sort direction (asc or desc)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing an array of GitLab groups if successful, or an error message if not</returns>
    public static async Task<Result<GitLabGroupDto[]>> GetSubgroups(
        string groupIdOrName, 
        string pat, 
        string gitlabDomain,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Retrieving subgroups for group {groupIdOrName}...");
            
            // First, resolve the ID if a name was provided
            string groupId = groupIdOrName;
            if (!int.TryParse(groupIdOrName, out _))
            {
                var groupResponse = await client.GetAsync($"https://{gitlabDomain}/api/v4/groups?search={groupIdOrName}");
                if (!groupResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await groupResponse.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroupDto[]>($"Failed to find group by name: {groupResponse.StatusCode}. {errorResponse}");
                }
                
                var groups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(
                    await groupResponse.Content.ReadAsStringAsync());
                
                if (groups == null || !groups.Any(g => !g.IsMarkedForDeletion))
                {
                    return Result.Failure<GitLabGroupDto[]>($"No active group found with name: {groupIdOrName}");
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
                    return Result.Failure<GitLabGroupDto[]>($"No active group found with name: {groupIdOrName}");
                }
            }
            
            // Validate orderBy parameter
            var validOrderByFields = new[] { "id", "name", "path", "created_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
            {
                orderBy = "name"; // Default to name if invalid
            }
            
            // Validate sort parameter
            sort = sort.ToLower() == "desc" ? "desc" : "asc";
            
            // Set up pagination and fetch all pages
            var allSubgroups = new List<GitLabGroupDto>();
            int page = 1;
            const int perPage = 100; // Maximum allowed by GitLab API
            bool hasMorePages = true;
            
            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of subgroups...");
                
                // Get subgroups with paging
                var url = $"https://{gitlabDomain}/api/v4/groups/{groupId}/subgroups" +
                         $"?per_page={perPage}" +
                         $"&page={page}" +
                         $"&order_by={orderBy}" +
                         $"&sort={sort}";
                         
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var subgroups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(responseBody);
                    
                    if (subgroups == null)
                    {
                        return Result.Failure<GitLabGroupDto[]>("Failed to deserialize subgroups data");
                    }
                    
                    // Add current page results to our collection, filtering out groups marked for deletion
                    allSubgroups.AddRange(subgroups.Where(g => !g.IsMarkedForDeletion));
                    
                    // Check if there are more pages
                    if (subgroups.Count < perPage)
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
                    return Result.Failure<GitLabGroupDto[]>($"Failed to get subgroups: {response.StatusCode}. {errorResponse}");
                }
            }
            
            // For each subgroup, check if it has subgroups
            onMessage?.Invoke($"Found {allSubgroups.Count} active subgroups, checking for nested subgroups...");
            
            var tasks = allSubgroups.Select(async g => {
                var hasSubgroupsCheck = await client.GetAsync($"https://{gitlabDomain}/api/v4/groups/{g.Id}/subgroups?per_page=1");
                if (hasSubgroupsCheck.IsSuccessStatusCode)
                {
                    var checkBody = await hasSubgroupsCheck.Content.ReadAsStringAsync();
                    var checkSubgroups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(checkBody);
                    // Only count active subgroups
                    g.HasSubgroups = checkSubgroups != null && checkSubgroups.Any(sg => !sg.IsMarkedForDeletion);
                }
                return g;
            });
            
            var groupsWithSubgroupInfo = await Task.WhenAll(tasks);
            onMessage?.Invoke($"Retrieved a total of {groupsWithSubgroupInfo.Length} active subgroups");
            
            return Result.Success(groupsWithSubgroupInfo);
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroupDto[]>($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Finds GitLab groups by name or ID
    /// </summary>
    /// <param name="nameOrId">The exact name or ID of the group to find</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="orderBy">Field to order by (id, name, path, or created_at)</param>
    /// <param name="sort">Sort direction (asc or desc)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing an array of GitLab groups if successful, or an error message if not</returns>
    public static async Task<Result<GitLabGroupDto[]>> FindGroups(
        string nameOrId, 
        string pat, 
        string gitlabDomain,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Finding groups with name or ID: {nameOrId}...");
            
            // If it's an ID, use a direct lookup
            if (int.TryParse(nameOrId, out int groupId))
            {
                var directResponse = await client.GetAsync($"https://{gitlabDomain}/api/v4/groups/{groupId}");
                
                if (directResponse.IsSuccessStatusCode)
                {
                    var responseBody = await directResponse.Content.ReadAsStringAsync();
                    var group = JsonSerializer.Deserialize<GitLabGroupDto>(responseBody);
                    
                    if (group == null)
                    {
                        return Result.Failure<GitLabGroupDto[]>("Failed to deserialize group data");
                    }
                    
                    if (group.IsMarkedForDeletion)
                    {
                        return Result.Success(Array.Empty<GitLabGroupDto>());
                    }
                    
                    onMessage?.Invoke($"Found 1 group with ID {nameOrId}");
                    return Result.Success(new[] { group });
                }
                else if (directResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    var errorResponse = await directResponse.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroupDto[]>($"Error searching for group by ID: {directResponse.StatusCode}. {errorResponse}");
                }
                // If not found by ID, continue with a name search
            }
            
            // Validate orderBy parameter
            var validOrderByFields = new[] { "id", "name", "path", "created_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
            {
                orderBy = "name"; // Default to name if invalid
            }
            
            // Validate sort parameter
            sort = sort.ToLower() == "desc" ? "desc" : "asc";
            
            // Set up pagination and fetch all pages
            var allGroups = new List<GitLabGroupDto>();
            int page = 1;
            const int perPage = 100; // Maximum allowed by GitLab API
            bool hasMorePages = true;
            
            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of groups...");
                
                // Use exact search by name
                var url = $"https://{gitlabDomain}/api/v4/groups" +
                         $"?per_page={perPage}" +
                         $"&page={page}" +
                         $"&order_by={orderBy}" +
                         $"&sort={sort}" +
                         $"&search={Uri.EscapeDataString(nameOrId)}";
                         
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var groups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(responseBody);
                    
                    if (groups == null)
                    {
                        return Result.Failure<GitLabGroupDto[]>("Failed to deserialize groups data");
                    }
                    
                    // Add current page results to our collection, filtering out groups marked for deletion
                    // and only include exact name matches (case insensitive)
                    var exactMatches = groups.Where(g => 
                        !g.IsMarkedForDeletion && 
                        (g.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || 
                         g.Path.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)));
                    
                    allGroups.AddRange(exactMatches);
                    
                    // Check if there are more pages
                    if (groups.Count < perPage)
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
                    return Result.Failure<GitLabGroupDto[]>($"Failed to search for groups: {response.StatusCode}. {errorResponse}");
                }
            }
            
            onMessage?.Invoke($"Found {allGroups.Count} group(s) matching '{nameOrId}'");
            return Result.Success(allGroups.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroupDto[]>($"An error occurred: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Searches for GitLab groups using a wildcard pattern
    /// </summary>
    /// <param name="searchPattern">The search pattern to match groups (supports partial matches)</param>
    /// <param name="pat">Personal Access Token for GitLab API</param>
    /// <param name="gitlabDomain">GitLab domain (e.g. gitlab.com)</param>
    /// <param name="orderBy">Field to order by (id, name, path, or created_at)</param>
    /// <param name="sort">Sort direction (asc or desc)</param>
    /// <param name="onMessage">Optional action to receive informational messages</param>
    /// <returns>A Result containing an array of GitLab groups if successful, or an error message if not</returns>
    public static async Task<Result<GitLabGroupDto[]>> SearchGroups(
        string searchPattern, 
        string pat, 
        string gitlabDomain,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);
        
        try
        {
            onMessage?.Invoke($"Searching for groups matching pattern: {searchPattern}...");
            
            // Validate orderBy parameter
            var validOrderByFields = new[] { "id", "name", "path", "created_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
            {
                orderBy = "name"; // Default to name if invalid
            }
            
            // Validate sort parameter
            sort = sort.ToLower() == "desc" ? "desc" : "asc";
            
            // Set up pagination and fetch all pages
            var allGroups = new List<GitLabGroupDto>();
            int page = 1;
            const int perPage = 100; // Maximum allowed by GitLab API
            bool hasMorePages = true;
            
            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of search results...");
                
                // Use wildcard search
                var url = $"https://{gitlabDomain}/api/v4/groups" +
                         $"?per_page={perPage}" +
                         $"&page={page}" +
                         $"&order_by={orderBy}" +
                         $"&sort={sort}" +
                         $"&search={Uri.EscapeDataString(searchPattern)}";
                         
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var groups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(responseBody);
                    
                    if (groups == null)
                    {
                        return Result.Failure<GitLabGroupDto[]>("Failed to deserialize groups data");
                    }
                    
                    // Add current page results to our collection, filtering out groups marked for deletion
                    allGroups.AddRange(groups.Where(g => !g.IsMarkedForDeletion));
                    
                    // Check if there are more pages
                    if (groups.Count < perPage)
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
                    return Result.Failure<GitLabGroupDto[]>($"Failed to search for groups: {response.StatusCode}. {errorResponse}");
                }
            }
            
            onMessage?.Invoke($"Found {allGroups.Count} group(s) matching pattern '{searchPattern}'");
            return Result.Success(allGroups.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroupDto[]>($"An error occurred: {ex.Message}");
        }
    }
}