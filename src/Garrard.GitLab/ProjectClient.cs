using System.Text.Json;
using CSharpFunctionalExtensions;
using Garrard.GitLab.Library.DTOs;
using Garrard.GitLab.Library.Http;
using Microsoft.Extensions.Options;

namespace Garrard.GitLab.Library;

/// <summary>
/// Instance client for working with GitLab projects and project variables.
/// </summary>
public sealed class ProjectClient
{
    private readonly IGitLabHttpClientFactory _factory;
    private readonly GitLabOptions _opts;

    internal string Domain => _opts.Domain;

    public ProjectClient(IGitLabHttpClientFactory factory, IOptions<GitLabOptions> options)
    {
        _factory = factory;
        _opts = options.Value;
    }

    /// <summary>
    /// Gets all projects within a GitLab group.
    /// </summary>
    public async Task<Result<GitLabProject[]>> GetProjectsInGroup(
        string groupIdOrName,
        bool includeSubgroups = true,
        string orderBy = "name",
        string sort = "asc",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Retrieving projects for group {groupIdOrName}...");

            string groupId = groupIdOrName;
            if (!int.TryParse(groupIdOrName, out _))
            {
                var groupResponse = await client.GetAsync($"https://{_opts.Domain}/api/v4/groups?search={Uri.EscapeDataString(groupIdOrName)}");
                if (!groupResponse.IsSuccessStatusCode)
                {
                    var errorResponse = await groupResponse.Content.ReadAsStringAsync();
                    return Result.Failure<GitLabProject[]>($"Failed to find group by name: {groupResponse.StatusCode}. {errorResponse}");
                }

                var groups = JsonSerializer.Deserialize<List<GitLabGroup>>(
                    await groupResponse.Content.ReadAsStringAsync());

                if (groups == null || !groups.Any(g => !g.IsMarkedForDeletion))
                    return Result.Failure<GitLabProject[]>($"No active group found with name: {groupIdOrName}");

                var activeGroup = groups.FirstOrDefault(g => !g.IsMarkedForDeletion);
                if (activeGroup != null)
                {
                    groupId = activeGroup.Id.ToString();
                    onMessage?.Invoke($"Resolved group name to ID: {groupId}");
                }
                else
                {
                    return Result.Failure<GitLabProject[]>($"No active group found with name: {groupIdOrName}");
                }
            }

            var validOrderByFields = new[] { "id", "name", "path", "created_at", "updated_at", "last_activity_at" };
            if (!validOrderByFields.Contains(orderBy.ToLower()))
                orderBy = "name";

            sort = sort.ToLower() == "desc" ? "desc" : "asc";

            var allProjects = new List<GitLabProject>();
            int page = 1;
            const int perPage = 100;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of projects...");

                var url = $"https://{_opts.Domain}/api/v4/groups/{groupId}/projects" +
                          $"?per_page={perPage}&page={page}&order_by={orderBy}&sort={sort}" +
                          $"&include_subgroups={includeSubgroups.ToString().ToLower()}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var projects = JsonSerializer.Deserialize<List<GitLabProject>>(responseBody);

                    if (projects == null)
                        return Result.Failure<GitLabProject[]>("Failed to deserialize projects data");

                    allProjects.AddRange(projects.Where(p => !p.IsMarkedForDeletion));

                    if (projects.Count < perPage)
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
                    return Result.Failure<GitLabProject[]>($"Failed to get projects: {response.StatusCode}. {errorResponse}");
                }
            }

            onMessage?.Invoke($"Retrieved a total of {allProjects.Count} active projects from group {groupIdOrName}");
            return Result.Success(allProjects.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabProject[]>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all variables for a GitLab project.
    /// </summary>
    public async Task<Result<GitLabVariable[]>> GetProjectVariables(
        string projectId,
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Retrieving variables for project {projectId}...");

            var allVariables = new List<GitLabVariable>();
            int page = 1;
            const int perPage = 100;
            bool hasMorePages = true;

            while (hasMorePages)
            {
                onMessage?.Invoke($"Fetching page {page} of project variables...");

                var url = $"https://{_opts.Domain}/api/v4/projects/{projectId}/variables" +
                          $"?per_page={perPage}&page={page}";

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var variables = JsonSerializer.Deserialize<List<GitLabVariable>>(responseBody);

                    if (variables == null)
                        return Result.Failure<GitLabVariable[]>("Failed to deserialize variables data");

                    allVariables.AddRange(variables);

                    if (variables.Count < perPage)
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
                    return Result.Failure<GitLabVariable[]>($"Failed to get project variables: {response.StatusCode}. {errorResponse}");
                }
            }

            onMessage?.Invoke($"Retrieved a total of {allVariables.Count} variables from project {projectId}");
            return Result.Success(allVariables.ToArray());
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabVariable[]>($"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates or updates a variable in a GitLab project.
    /// </summary>
    public async Task<Result<GitLabVariable>> CreateOrUpdateProjectVariable(
        string projectId,
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
            onMessage?.Invoke($"Checking if variable {variableKey} exists in project {projectId}...");

            var getResult = await GetProjectVariable(projectId, variableKey, environmentScope);
            bool variableExists = !getResult.IsFailure;

            string url = $"https://{_opts.Domain}/api/v4/projects/{projectId}/variables";
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
                onMessage?.Invoke("Variable does not exist, creating...");
                response = await client.PostAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabVariable>(responseBody);

                if (variable == null)
                    return Result.Failure<GitLabVariable>("Failed to deserialize variable data");

                onMessage?.Invoke($"Successfully {(variableExists ? "updated" : "created")} variable {variableKey}");
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

    /// <summary>
    /// Gets a specific variable from a GitLab project.
    /// </summary>
    public async Task<Result<GitLabVariable>> GetProjectVariable(
        string projectId,
        string variableKey,
        string? environmentScope = "*",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Retrieving variable {variableKey} for project {projectId}...");

            string url = $"https://{_opts.Domain}/api/v4/projects/{projectId}/variables/{variableKey}";
            if (environmentScope != null && environmentScope != "*")
                url += $"?filter[environment_scope]={Uri.EscapeDataString(environmentScope)}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var variable = JsonSerializer.Deserialize<GitLabVariable>(responseBody);

                if (variable == null)
                    return Result.Failure<GitLabVariable>("Failed to deserialize variable data");

                onMessage?.Invoke($"Successfully retrieved variable {variableKey}");
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
    /// Deletes a variable from a GitLab project.
    /// </summary>
    public async Task<Result> DeleteProjectVariable(
        string projectId,
        string variableKey,
        string? environmentScope = "*",
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Deleting variable {variableKey} from project {projectId}...");

            string url = $"https://{_opts.Domain}/api/v4/projects/{projectId}/variables/{variableKey}";
            if (environmentScope != null && environmentScope != "*")
                url += $"?filter[environment_scope]={Uri.EscapeDataString(environmentScope)}";

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

    /// <summary>
    /// Creates a new GitLab project.
    /// </summary>
    public async Task<Result<GitLabProject>> CreateGitLabProject(
        string name,
        int? parentGroupId = null,
        bool? enableInstanceRunners = null,
        Action<string>? onMessage = null)
    {
        var client = _factory.CreateClient();

        try
        {
            onMessage?.Invoke($"Creating GitLab project '{name}'...");

            var payload = new Dictionary<string, object> { { "name", name } };

            if (parentGroupId.HasValue)
            {
                payload.Add("namespace_id", parentGroupId.Value);
                onMessage?.Invoke($"Project will be created in group ID: {parentGroupId.Value}");
            }

            if (enableInstanceRunners.HasValue)
            {
                payload.Add("shared_runners_enabled", enableInstanceRunners.Value);
                onMessage?.Invoke($"Instance runners will be {(enableInstanceRunners.Value ? "enabled" : "disabled")}");
            }

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"https://{_opts.Domain}/api/v4/projects", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var project = JsonSerializer.Deserialize<GitLabProject>(responseBody);

                if (project == null)
                    return Result.Failure<GitLabProject>("Failed to deserialize project data");

                onMessage?.Invoke($"Successfully created project '{project.Name}' (ID: {project.Id})");
                return Result.Success(project);
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return Result.Failure<GitLabProject>($"Failed to create project: {response.StatusCode}. {errorResponse}");
            }
        }
        catch (Exception ex)
        {
            return Result.Failure<GitLabProject>($"An error occurred: {ex.Message}");
        }
    }
}
