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
                
                if (groups == null || !groups.Any())
                {
                    return Result.Failure<GitLabGroupDto[]>($"No group found with name: {groupIdOrName}");
                }
                
                // Use the first match (most relevant)
                groupId = groups[0].Id.ToString();
                onMessage?.Invoke($"Resolved group name to ID: {groupId}");
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
                    
                    // Add current page results to our collection
                    allSubgroups.AddRange(subgroups);
                    
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
            onMessage?.Invoke($"Found {allSubgroups.Count} subgroups, checking for nested subgroups...");
            
            var tasks = allSubgroups.Select(async g => {
                var hasSubgroupsCheck = await client.GetAsync($"https://{gitlabDomain}/api/v4/groups/{g.Id}/subgroups?per_page=1");
                if (hasSubgroupsCheck.IsSuccessStatusCode)
                {
                    var checkBody = await hasSubgroupsCheck.Content.ReadAsStringAsync();
                    var checkSubgroups = JsonSerializer.Deserialize<List<GitLabGroupDto>>(checkBody);
                    g.HasSubgroups = checkSubgroups != null && checkSubgroups.Any();
                }
                return g;
            });
            
            var groupsWithSubgroupInfo = await Task.WhenAll(tasks);
            onMessage?.Invoke($"Retrieved a total of {groupsWithSubgroupInfo.Length} subgroups");
            
            return Result.Success(groupsWithSubgroupInfo);
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroupDto[]>($"An error occurred: {ex.Message}");
        }
    }
}