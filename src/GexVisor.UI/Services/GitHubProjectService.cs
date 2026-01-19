using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GexVisor.UI.Models;

namespace GexVisor.UI.Services;

/// <summary>
/// Service for interacting with GitHub Projects v2 via GraphQL API.
/// </summary>
public class GitHubProjectService
{
    private readonly HttpClient _http;
    private readonly GitHubAuthService _auth;
    private readonly ILocalStorageService _storage;

    public event Action? OnProjectsChanged;

    private List<GitHubProject> _projects = new();
    private GitHubProject? _selectedProject;

    public IReadOnlyList<GitHubProject> Projects => _projects;
    public GitHubProject? SelectedProject => _selectedProject;

    public GitHubProjectService(HttpClient http, GitHubAuthService auth, LocalStorageService storage)
    {
        _http = http;
        _auth = auth;
        _storage = storage;
    }

    /// <summary>
    /// Load cached projects from localStorage.
    /// </summary>
    public async Task LoadAsync()
    {
        var stored = await _storage.GetAsync<GitHubProjectSettings>(StorageKeys.GitHubProjects);
        if (stored != null)
        {
            _projects = stored.Projects ?? new();
            _selectedProject = _projects.FirstOrDefault(p => p.Id == stored.SelectedProjectId);
            OnProjectsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Fetch all projects the user has access to.
    /// </summary>
    public async Task<List<GitHubProject>> FetchProjectsAsync()
    {
        if (!_auth.IsAuthenticated)
            return new();

        var query = """
            query {
                viewer {
                    login
                    projectsV2(first: 20) {
                        nodes {
                            id
                            title
                            shortDescription
                            url
                            closed
                            items(first: 1) {
                                totalCount
                            }
                            fields(first: 20) {
                                nodes {
                                    ... on ProjectV2Field {
                                        id
                                        name
                                    }
                                    ... on ProjectV2SingleSelectField {
                                        id
                                        name
                                        options {
                                            id
                                            name
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """;

        var response = await ExecuteGraphQLAsync<ViewerProjectsResponse>(query);
        if (response?.Data?.Viewer?.ProjectsV2?.Nodes != null)
        {
            _projects = response.Data.Viewer.ProjectsV2.Nodes
                .Where(p => p != null && !p.Closed)
                .Select(p => new GitHubProject
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.ShortDescription,
                    Url = p.Url,
                    ItemCount = p.Items?.TotalCount ?? 0,
                    StatusField = p.Fields?.Nodes?
                        .Where(f => f.Name?.ToLower() == "status" && f.Options != null)
                        .Select(f => new GitHubStatusField
                        {
                            Id = f.Id,
                            Name = f.Name ?? "Status",
                            Options = f.Options!.Select(o => new GitHubStatusOption
                            {
                                Id = o.Id,
                                Name = o.Name
                            }).ToList()
                        })
                        .FirstOrDefault()
                })
                .ToList();

            await SaveAsync();
            OnProjectsChanged?.Invoke();
        }

        return _projects;
    }

    /// <summary>
    /// Select a project for sync.
    /// </summary>
    public async Task SelectProjectAsync(string projectId)
    {
        _selectedProject = _projects.FirstOrDefault(p => p.Id == projectId);
        await SaveAsync();
        OnProjectsChanged?.Invoke();
    }

    /// <summary>
    /// Clear the selected project.
    /// </summary>
    public async Task ClearSelectionAsync()
    {
        _selectedProject = null;
        await SaveAsync();
        OnProjectsChanged?.Invoke();
    }

    /// <summary>
    /// Fetch items from the selected project.
    /// </summary>
    public async Task<List<GitHubProjectItem>> FetchProjectItemsAsync()
    {
        if (!_auth.IsAuthenticated || _selectedProject == null)
            return new();

        var query = """
            query($projectId: ID!) {
                node(id: $projectId) {
                    ... on ProjectV2 {
                        items(first: 100) {
                            nodes {
                                id
                                content {
                                    ... on Issue {
                                        id
                                        title
                                        body
                                        state
                                        url
                                        labels(first: 10) {
                                            nodes {
                                                name
                                                color
                                            }
                                        }
                                    }
                                    ... on DraftIssue {
                                        id
                                        title
                                        body
                                    }
                                }
                                fieldValues(first: 10) {
                                    nodes {
                                        ... on ProjectV2ItemFieldSingleSelectValue {
                                            field {
                                                ... on ProjectV2SingleSelectField {
                                                    name
                                                }
                                            }
                                            name
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """;

        var variables = new { projectId = _selectedProject.Id };
        var response = await ExecuteGraphQLAsync<ProjectItemsResponse>(query, variables);

        if (response?.Data?.Node?.Items?.Nodes == null)
            return new();

        return response.Data.Node.Items.Nodes
            .Where(n => n.Content != null)
            .Select(n => new GitHubProjectItem
            {
                Id = n.Id,
                ContentId = n.Content!.Id,
                Title = n.Content.Title,
                Body = n.Content.Body,
                State = n.Content.State,
                Url = n.Content.Url,
                Status = n.FieldValues?.Nodes?
                    .FirstOrDefault(f => f.Field?.Name?.ToLower() == "status")?.Name,
                Labels = n.Content.Labels?.Nodes?
                    .Select(l => l.Name).ToList() ?? new()
            })
            .ToList();
    }

    /// <summary>
    /// Execute a GraphQL query against the GitHub API via proxy.
    /// Throws exceptions for network errors, auth failures, or JSON parsing errors
    /// to allow proper error handling by callers (BoardStateService).
    /// </summary>
    private async Task<T?> ExecuteGraphQLAsync<T>(string query, object? variables = null) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GitHubAppConfig.GraphQLUrl);
        request.Headers.Authorization = _auth.GetAuthHeader();

        var body = new { query, variables };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode(); // Throws HttpRequestException on failure

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task SaveAsync()
    {
        var settings = new GitHubProjectSettings
        {
            Projects = _projects,
            SelectedProjectId = _selectedProject?.Id
        };
        await _storage.SetAsync(StorageKeys.GitHubProjects, settings);
    }
}

// GraphQL response types
internal class ViewerProjectsResponse
{
    [JsonPropertyName("data")]
    public ViewerData? Data { get; set; }
}

internal class ViewerData
{
    [JsonPropertyName("viewer")]
    public Viewer? Viewer { get; set; }
}

internal class Viewer
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("projectsV2")]
    public ProjectsV2Connection? ProjectsV2 { get; set; }
}

internal class ProjectsV2Connection
{
    [JsonPropertyName("nodes")]
    public List<ProjectV2Node>? Nodes { get; set; }
}

internal class ProjectV2Node
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }

    [JsonPropertyName("items")]
    public ItemsCount? Items { get; set; }

    [JsonPropertyName("fields")]
    public FieldsConnection? Fields { get; set; }
}

internal class ItemsCount
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

internal class FieldsConnection
{
    [JsonPropertyName("nodes")]
    public List<FieldNode>? Nodes { get; set; }
}

internal class FieldNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("options")]
    public List<FieldOption>? Options { get; set; }
}

internal class FieldOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

internal class ProjectItemsResponse
{
    [JsonPropertyName("data")]
    public ProjectItemsData? Data { get; set; }
}

internal class ProjectItemsData
{
    [JsonPropertyName("node")]
    public ProjectNode? Node { get; set; }
}

internal class ProjectNode
{
    [JsonPropertyName("items")]
    public ProjectItemsConnection? Items { get; set; }
}

internal class ProjectItemsConnection
{
    [JsonPropertyName("nodes")]
    public List<ProjectItemNode>? Nodes { get; set; }
}

internal class ProjectItemNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("content")]
    public ItemContent? Content { get; set; }

    [JsonPropertyName("fieldValues")]
    public FieldValuesConnection? FieldValues { get; set; }
}

internal class ItemContent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("labels")]
    public LabelsConnection? Labels { get; set; }
}

internal class LabelsConnection
{
    [JsonPropertyName("nodes")]
    public List<LabelNode>? Nodes { get; set; }
}

internal class LabelNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

internal class FieldValuesConnection
{
    [JsonPropertyName("nodes")]
    public List<FieldValueNode>? Nodes { get; set; }
}

internal class FieldValueNode
{
    [JsonPropertyName("field")]
    public FieldRef? Field { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal class FieldRef
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
