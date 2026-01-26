namespace GexVisor.UI.Models;

/// <summary>
/// A GitHub Project (v2) board.
/// </summary>
public class GitHubProject
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Url { get; set; } = "";
    public int ItemCount { get; set; }
    public GitHubStatusField? StatusField { get; set; }
}

/// <summary>
/// A status field from a GitHub Project.
/// </summary>
public class GitHubStatusField
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<GitHubStatusOption> Options { get; set; } = new();
}

/// <summary>
/// A status option (column) in a GitHub Project.
/// </summary>
public class GitHubStatusOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

/// <summary>
/// An item (issue/draft) in a GitHub Project.
/// </summary>
public class GitHubProjectItem
{
    public string Id { get; set; } = "";
    public string ContentId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public string? State { get; set; }
    public string? Url { get; set; }
    public string? Status { get; set; }
    public List<string> Labels { get; set; } = new();
}

/// <summary>
/// Settings for GitHub project sync stored in localStorage.
/// </summary>
public class GitHubProjectSettings
{
    public List<GitHubProject>? Projects { get; set; }
    public string? SelectedProjectId { get; set; }
    public string? EndCursor { get; set; }
    public bool HasMoreProjects { get; set; }
    public string? CurrentOrg { get; set; }
    public List<string> RecentOrgs { get; set; } = new();
}
