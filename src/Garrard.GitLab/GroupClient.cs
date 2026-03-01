using System.Text.Json;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Garrard.GitLab.Library.DTOs;
using Garrard.GitLab.Library.Http;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Library;

/// <summary>
/// Instance client for working with GitLab groups.
/// </summary>
public sealed class GroupClient
{
    private static readonly Regex InvalidPathCharsRegex =
        new Regex(@"[^a-z0-9\-_.]", RegexOptions.Compiled);

    private static readonly Regex ConsecutiveSpecialCharsRegex =
        new Regex(@"[\-_.]{2,}", RegexOptions.Compiled);

    private readonly IGitLabHttpClientFactory _factory;
    private readonly GitLabOptions _opts;

    internal string Domain => _opts.Domain;

    public GroupClient(IGitLabHttpClientFactory factory, IOptions<GitLabOptions> options)
    {
        _factory = factory;
        _opts = options.Value;
    }

    /// <summary>
    /// Gets all GitLab groups beneath a specified group.
    /// </summary>
    public async Task<Result<GitLabGroup[]>> GetSubgroups(
        string groupIdOrName,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Retrieving subgroups for group {groupIdOrName}...");

            string groupId = groupIdOrName;
            if (!int.TryParse(groupIdOrName, out _))
            {
                var groupResponse = await client.GetAsync($"https://{_opts.Domain}/api/v4/groups?search={Uri.EscapeDataString(groupIdOrName)}");
                if (!groupResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await groupResponse.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroup[]>($"Failed to find group by name: {groupResponse.StatusCode}. {errorResponse}");
                }

                var groups = JsonSerializer.Deserialize<List<GitLabGroup>>(
                    await groupResponse.Content.ReadAsStringAsync());

                if (groups == null || !groups.Any(g => !g.IsMarkedForDeletion))
                    return Result.Failure<GitLabGroup[]>($"No active group found with name: {groupIdOrName}");

                var activeGroup = groups.FirstOrDefault(g => !g.IsMarkedForDeletion);
                if (activeGroup != null)
                {
                    groupId = activeGroup.Id.ToString();
                    onMessage?.Invoke($"Resolved group name to ID: {groupId}");
                }
                else
                {
                    return Result.Failure<GitLabGroup[]>($"No active group found with name: {groupIdOrName}");
                }
            }

            var validOrderByFields = new[] { "id", "name", "path", "created_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
                orderBy = "name";

            sort = sort.ToLower() == "desc" ? "desc" : "asc";

            var allSubgroups = new List<GitLabGroup>();
            int page = 1;
            const int perPage = 100;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of subgroups...");

                var url = $"https://{_opts.Domain}/api/v4/groups/{groupId}/subgroups" +
                          $"?per_page={perPage}&page={page}&order_by={orderBy}&sort={sort}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var subgroups = JsonSerializer.Deserialize<List<GitLabGroup>>(responseBody);

                    if (subgroups == null)
                        return Result.Failure<GitLabGroup[]>("Failed to deserialize subgroups data");

                    allSubgroups.AddRange(subgroups.Where(g => !g.IsMarkedForDeletion));

                    if (subgroups.Count < perPage)
                    {
                        hasMorePages = false;
                    }
                    else
                    {
                        if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesHeader))
                        {
                            if (int.TryParse(totalPagesHeader.FirstOrDefault(), out int totalPages))
                                hasMorePages = page < totalPages;
                        }
                        page++;
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroup[]>($"Failed to get subgroups: {response.StatusCode}. {errorResponse}");
                }
            }

            onMessage?.Invoke($"Found {allSubgroups.Count} active subgroups, checking for nested subgroups...");

            var tasks = allSubgroups.Select(async g =>
            {
                var checkClient = _factory.CreateClient();
                var hasSubgroupsCheck = await checkClient.GetAsync($"https://{_opts.Domain}/api/v4/groups/{g.Id}/subgroups?per_page=1");
                if (hasSubgroupsCheck.IsSuccessStatusCode)
                {
                    var checkBody = await hasSubgroupsCheck.Content.ReadAsStringAsync();
                    var checkSubgroups = JsonSerializer.Deserialize<List<GitLabGroup>>(checkBody);
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
            return Result.Failure<GitLabGroup[]>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds GitLab groups by name or ID.
    /// </summary>
    public async Task<Result<GitLabGroup[]>> FindGroups(
        string nameOrId,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Finding groups with name or ID: {nameOrId}...");

            if (int.TryParse(nameOrId, out int groupId))
            {
                var directResponse = await client.GetAsync($"https://{_opts.Domain}/api/v4/groups/{groupId}");

                if (directResponse.IsSuccessStatusCode)
                {
                    var responseBody = await directResponse.Content.ReadAsStringAsync();
                    var group = JsonSerializer.Deserialize<GitLabGroup>(responseBody);

                    if (group == null)
                        return Result.Failure<GitLabGroup[]>("Failed to deserialize group data");

                    if (group.IsMarkedForDeletion)
                        return Result.Success(Array.Empty<GitLabGroup>());

                    onMessage?.Invoke($"Found 1 group with ID {nameOrId}");
                    return Result.Success(new[] { group });
                }
                else if (directResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    var errorResponse = await directResponse.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroup[]>($"Error searching for group by ID: {directResponse.StatusCode}. {errorResponse}");
                }
            }

            var validOrderByFields = new[] { "id", "name", "path", "created_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
                orderBy = "name";

            sort = sort.ToLower() == "desc" ? "desc" : "asc";

            var allGroups = new List<GitLabGroup>();
            int page = 1;
            const int perPage = 100;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of groups...");

                var url = $"https://{_opts.Domain}/api/v4/groups" +
                          $"?per_page={perPage}&page={page}&order_by={orderBy}&sort={sort}" +
                          $"&search={Uri.EscapeDataString(nameOrId)}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var groups = JsonSerializer.Deserialize<List<GitLabGroup>>(responseBody);

                    if (groups == null)
                        return Result.Failure<GitLabGroup[]>("Failed to deserialize groups data");

                    var exactMatches = groups.Where(g =>
                        !g.IsMarkedForDeletion &&
                        (g.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) ||
                         g.Path.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)));

                    allGroups.AddRange(exactMatches);

                    if (groups.Count < perPage)
                    {
                        hasMorePages = false;
                    }
                    else
                    {
                        if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesHeader))
                        {
                            if (int.TryParse(totalPagesHeader.FirstOrDefault(), out int totalPages))
                                hasMorePages = page < totalPages;
                        }
                        page++;
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroup[]>($"Failed to search for groups: {response.StatusCode}. {errorResponse}");
                }
            }

            onMessage?.Invoke($"Found {allGroups.Count} group(s) matching '{nameOrId}'");
            return Result.Success(allGroups.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroup[]>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new GitLab group.
    /// </summary>
    public async Task<Result<GitLabGroup>> CreateGitLabGroup(
        string name,
        int? parentId = null,
        Action<string>? onMessage = null)
    {
        using var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Creating GitLab group: {name}...");

            var fields = new List<KeyValuePair<string, string>>
            {
                new("name", name),
                new("path", SanitizePathFromName(name))
            };

            if (parentId.HasValue)
            {
                fields.Add(new("parent_id", parentId.Value.ToString()));
                onMessage?.Invoke($"Setting parent group ID to: {parentId.Value}");
            }

            var content = new FormUrlEncodedContent(fields);
            var url = $"https://{_opts.Domain}/api/v4/groups";

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var group = JsonSerializer.Deserialize<GitLabGroup>(responseBody, options);

                if (group == null)
                    return Result.Failure<GitLabGroup>("Failed to deserialize group data");

                onMessage?.Invoke($"Successfully created group '{name}' with ID: {group.Id}");
                return Result.Success(group);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return Result.Failure<GitLabGroup>($"Failed to create group: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroup>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for GitLab groups using a wildcard pattern.
    /// </summary>
    public async Task<Result<GitLabGroup[]>> SearchGroups(
        string searchPattern,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Searching for groups matching pattern: {searchPattern}...");

            var validOrderByFields = new[] { "id", "name", "path", "created_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
                orderBy = "name";

            sort = sort.ToLower() == "desc" ? "desc" : "asc";

            var allGroups = new List<GitLabGroup>();
            int page = 1;
            const int perPage = 100;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of search results...");

                var url = $"https://{_opts.Domain}/api/v4/groups" +
                          $"?per_page={perPage}&page={page}&order_by={orderBy}&sort={sort}" +
                          $"&search={Uri.EscapeDataString(searchPattern)}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var groups = JsonSerializer.Deserialize<List<GitLabGroup>>(responseBody);

                    if (groups == null)
                        return Result.Failure<GitLabGroup[]>("Failed to deserialize groups data");

                    allGroups.AddRange(groups.Where(g => !g.IsMarkedForDeletion));

                    if (groups.Count < perPage)
                    {
                        hasMorePages = false;
                    }
                    else
                    {
                        if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesHeader))
                        {
                            if (int.TryParse(totalPagesHeader.FirstOrDefault(), out int totalPages))
                                hasMorePages = page < totalPages;
                        }
                        page++;
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabGroup[]>($"Failed to search for groups: {response.StatusCode}. {errorResponse}");
                }
            }

            onMessage?.Invoke($"Found {allGroups.Count} group(s) matching pattern '{searchPattern}'");
            return Result.Success(allGroups.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabGroup[]>($"An error occurred: {ex.Message}");
        }
    }

    private static string SanitizePathFromName(string name)
    {
        var path = name.ToLower();
        path = InvalidPathCharsRegex.Replace(path, "-");
        path = ConsecutiveSpecialCharsRegex.Replace(path, "-");
        path = path.Trim('-', '_', '.');

        if (string.IsNullOrWhiteSpace(path))
            path = "new-group";

        return path;
    }
}
