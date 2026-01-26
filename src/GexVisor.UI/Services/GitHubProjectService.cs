using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GexVisor.UI.Configuration;
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
    public event Action<string>? OnError;

    private List<GitHubProject> _projects = new();
    private GitHubProject? _selectedProject;
    private string? _endCursor;
    private bool _hasMoreProjects;
    private string? _currentOrg;
    private List<string> _recentOrgs = new();

    public IReadOnlyList<GitHubProject> Projects => _projects;
    public GitHubProject? SelectedProject => _selectedProject;
    public bool HasMoreProjects => _hasMoreProjects;
    public string? CurrentOrg => _currentOrg;
    public IReadOnlyList<string> RecentOrgs => _recentOrgs;

    public GitHubProjectService(HttpClient http, GitHubAuthService auth, ILocalStorageService storage)
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
        var stored = await _storage.GetAsync<GitHubProjectSettings>(AppConstants.Storage.GitHubProjects);
        if (stored != null)
        {
            _projects = stored.Projects ?? new();
            _selectedProject = _projects.FirstOrDefault(p => p.Id == stored.SelectedProjectId);
            _endCursor = stored.EndCursor;
            _hasMoreProjects = stored.HasMoreProjects;
            _currentOrg = stored.CurrentOrg;
            _recentOrgs = stored.RecentOrgs ?? new();
            OnProjectsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Fetch projects the user has access to. Pass afterCursor to fetch subsequent pages.
    /// </summary>
    public async Task<List<GitHubProject>> FetchProjectsAsync(string? afterCursor = null)
    {
        if (!_auth.IsAuthenticated)
        {
            return new();
        }

        try
        {
            var query = $$"""
                query($cursor: String) {
                    viewer {
                        login
                        projectsV2(first: {{AppConstants.GitHub.MaxProjectsPerQuery}}, after: $cursor) {
                            pageInfo {
                                endCursor
                                hasNextPage
                            }
                            nodes {
                                id
                                title
                                shortDescription
                                url
                                closed
                                items(first: 1) {
                                    totalCount
                                }
                                fields(first: {{AppConstants.GitHub.MaxFieldsPerProject}}) {
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

            var variables = afterCursor != null ? new { cursor = afterCursor } : null;
            var response = await ExecuteGraphQLAsync<ViewerProjectsResponse>(query, variables);

            if (response?.Errors != null && response.Errors.Count > 0)
            {
                var errorMsg = string.Join("; ", response.Errors.Select(e => e.Message));
                OnError?.Invoke($"GitHub API Error: {errorMsg}");
                return new();
            }

            if (response?.Data?.Viewer?.ProjectsV2 != null)
            {
                var connection = response.Data.Viewer.ProjectsV2;

                // Update pagination state
                _endCursor = connection.PageInfo?.EndCursor;
                _hasMoreProjects = connection.PageInfo?.HasNextPage ?? false;

                var newProjects = MapProjects(connection.Nodes);

                // Fresh fetch replaces list, pagination appends
                if (afterCursor == null)
                {
                    _projects = newProjects;
                }
                else
                {
                    // Dedupe by ID when appending
                    var existingIds = _projects.Select(p => p.Id).ToHashSet();
                    _projects.AddRange(newProjects.Where(p => !existingIds.Contains(p.Id)));
                }

                _currentOrg = null;
                await SaveAsync();
                OnProjectsChanged?.Invoke();
                return newProjects;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to fetch projects: {ex.Message}");
        }

        return new();
    }

    /// <summary>
    /// Fetch projects for a specific organization.
    /// </summary>
    public async Task<List<GitHubProject>> FetchOrganizationProjectsAsync(string orgName, string? afterCursor = null)
    {
        if (!_auth.IsAuthenticated)
        {
            return new();
        }

        try
        {
            var query = $$"""
                query($login: String!, $cursor: String) {
                    organization(login: $login) {
                        projectsV2(first: {{AppConstants.GitHub.MaxProjectsPerQuery}}, after: $cursor) {
                            pageInfo {
                                endCursor
                                hasNextPage
                            }
                            nodes {
                                id
                                title
                                shortDescription
                                url
                                closed
                                items(first: 1) {
                                    totalCount
                                }
                                fields(first: {{AppConstants.GitHub.MaxFieldsPerProject}}) {
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

            var variables = new { login = orgName, cursor = afterCursor };
            var response = await ExecuteGraphQLAsync<OrganizationProjectsResponse>(query, variables);

            if (response?.Errors != null && response.Errors.Count > 0)
            {
                var errorMsg = string.Join("; ", response.Errors.Select(e => e.Message));
                OnError?.Invoke($"GitHub API Error: {errorMsg}");
                return new();
            }

            if (response?.Data?.Organization?.ProjectsV2 != null)
            {
                var connection = response.Data.Organization.ProjectsV2;
                _endCursor = connection.PageInfo?.EndCursor;
                _hasMoreProjects = connection.PageInfo?.HasNextPage ?? false;

                var newProjects = MapProjects(connection.Nodes);

                if (afterCursor == null)
                {
                    _projects = newProjects;
                }
                else
                {
                    var existingIds = _projects.Select(p => p.Id).ToHashSet();
                    _projects.AddRange(newProjects.Where(p => !existingIds.Contains(p.Id)));
                }

                _currentOrg = orgName;
                AddToRecentOrgs(orgName);
                await SaveAsync();
                OnProjectsChanged?.Invoke();
                return newProjects;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to fetch org projects: {ex.Message}");
        }

        return new();
    }

    /// <summary>
    /// Fetch the next page of projects using cursor-based pagination.
    /// </summary>
    public async Task<List<GitHubProject>> FetchMoreProjectsAsync()
    {
        if (!_auth.IsAuthenticated || !_hasMoreProjects || _endCursor == null)
        {
            return new();
        }

        if (_currentOrg != null)
        {
            return await FetchOrganizationProjectsAsync(_currentOrg, _endCursor);
        }

        return await FetchProjectsAsync(_endCursor);
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
        {
            return new();
        }

        try
        {
            var query = $$"""
                query($projectId: ID!) {
                    node(id: $projectId) {
                        ... on ProjectV2 {
                            items(first: {{AppConstants.GitHub.MaxItemsPerProject}}) {
                                nodes {
                                    id
                                    content {
                                        ... on Issue {
                                            id
                                            title
                                            body
                                            state
                                            url
                                            labels(first: {{AppConstants.GitHub.MaxLabelsPerIssue}}) {
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
                                    fieldValues(first: {{AppConstants.GitHub.MaxFieldValuesPerItem}}) {
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

            if (response?.Errors != null && response.Errors.Count > 0)
            {
                var errorMsg = string.Join("; ", response.Errors.Select(e => e.Message));
                OnError?.Invoke($"GitHub API Error: {errorMsg}");
                return new();
            }

            if (response?.Data?.Node?.Items?.Nodes == null)
            {
                return new();
            }

            return MapProjectItems(response.Data.Node.Items.Nodes);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to fetch items: {ex.Message}");
            return new();
        }
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
            SelectedProjectId = _selectedProject?.Id,
            EndCursor = _endCursor,
            HasMoreProjects = _hasMoreProjects,
            CurrentOrg = _currentOrg,
            RecentOrgs = _recentOrgs
        };
        await _storage.SetAsync(AppConstants.Storage.GitHubProjects, settings);
    }

    private List<GitHubProject> MapProjects(List<ProjectV2Node>? nodes)
    {
        return nodes?
            .Where(p => p != null && !p.Closed)
            .Select(p => new GitHubProject
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.ShortDescription,
                Url = p.Url,
                ItemCount = p.Items?.TotalCount ?? 0,
                StatusField = p.Fields?.Nodes?
                    .Where(f => f.Name?.ToLower() == AppConstants.GitHub.StatusFieldName && f.Options != null)
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
            .ToList() ?? new();
    }

    private List<GitHubProjectItem> MapProjectItems(List<ProjectItemNode>? nodes)
    {
        return nodes?
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
                    .FirstOrDefault(f => f.Field?.Name?.ToLower() == AppConstants.GitHub.StatusFieldName)?.Name,
                Labels = n.Content.Labels?.Nodes?
                    .Select(l => l.Name).ToList() ?? new()
            })
            .ToList() ?? new();
    }

    public async Task ClearRecentOrgsAsync()
    {
        _recentOrgs.Clear();
        await SaveAsync();
        OnProjectsChanged?.Invoke();
    }

    private void AddToRecentOrgs(string orgName)
    {
        if (string.IsNullOrWhiteSpace(orgName))
        {
            return;
        }

        // Remove existing entry (case-insensitive) to move it to top
        _recentOrgs.RemoveAll(o => o.Equals(orgName, StringComparison.OrdinalIgnoreCase));

        _recentOrgs.Insert(0, orgName);

        if (_recentOrgs.Count > 5)
        {
            _recentOrgs.RemoveAt(_recentOrgs.Count - 1);
        }
    }
}

// GraphQL response types
internal class ViewerProjectsResponse
{
    [JsonPropertyName("data")]
    public ViewerData? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }
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

internal class OrganizationProjectsResponse
{
    [JsonPropertyName("data")]
    public OrganizationData? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }
}

internal class OrganizationData
{
    [JsonPropertyName("organization")]
    public Organization? Organization { get; set; }
}

internal class Organization
{
    [JsonPropertyName("projectsV2")]
    public ProjectsV2Connection? ProjectsV2 { get; set; }
}

internal class ProjectsV2Connection
{
    [JsonPropertyName("nodes")]
    public List<ProjectV2Node>? Nodes { get; set; }

    [JsonPropertyName("pageInfo")]
    public PageInfo? PageInfo { get; set; }
}

internal class PageInfo
{
    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; set; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }
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

    [JsonPropertyName("errors")]
    public List<GraphQLError>? Errors { get; set; }
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

internal class GraphQLError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}
