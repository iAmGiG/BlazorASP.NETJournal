# GitHub Integration

GexVisor includes built-in GitHub Projects v2 integration, allowing you to sync your research
tasks between the local TaskBoard and GitHub project boards.

## Features

### TaskBoard GitHub Integration

The Research Task Board (`/tasks`) provides seamless integration with GitHub Projects:

- **OAuth Authentication** - Secure device flow authentication with GitHub
- **Project Selector** - Browse and select from your GitHub Projects v2 boards
- **Dynamic Kanban Board** - Automatically syncs with GitHub project status fields
- **Unified View** - Normalize different status naming conventions across projects
- **Custom Status Mapping** - Override auto-detected status mappings

## Setup Instructions

### Prerequisites

1. **GitHub Account** with access to GitHub Projects v2
2. **GitHub CLI** (`gh`) installed and authenticated
3. **Personal Access Token** with the following scopes:
   - `project` - Read and write access to projects
   - `repo` - Access to repository issues

### Initial Setup

1. Navigate to the TaskBoard page at `/tasks`
2. Click the **"GitHub: Connect"** button
3. Follow the OAuth device flow:
   - Copy the provided code
   - Open the GitHub authorization URL
   - Paste the code and authorize the application
4. Once authenticated, select a project from the dropdown

## Architecture

### Services

#### GitHubAuthService

- **Purpose**: Handles OAuth device flow authentication
- **Location**: `src/GexVisor.UI/Services/GitHubAuthService.cs`
- **Storage**: Stores access tokens in localStorage
- **Security**: Tokens are scoped to the minimum required permissions

#### GitHubProjectService

- **Purpose**: GraphQL client for GitHub Projects v2 API
- **Location**: `src/GexVisor.UI/Services/GitHubProjectService.cs`
- **Features**:
  - Fetch user's projects
  - Load project items with custom fields
  - Extract status field options
  - Pagination support (max 100 items per request)

#### BoardStateService

- **Purpose**: Manages project board state with caching
- **Location**: `src/GexVisor.UI/Services/BoardStateService.cs`
- **Features**:
  - 5-minute cache expiry
  - Refresh on demand
  - Statistics (item counts, last refresh time)

#### StatusMapper

- **Purpose**: Normalizes GitHub status names to standard columns
- **Location**: `src/GexVisor.UI/Services/StatusMapper.cs`
- **Features**:
  - 30+ inference patterns (e.g., "in progress", "doing", "wip")
  - Custom mapping overrides per project
  - Word boundary matching (prevents "not started" matching "started")
  - Persistence to localStorage

### Components

#### GitHubAuthPanel

- **Purpose**: OAuth login UI
- **Location**: `src/GexVisor.UI/Components/GitHub/GitHubAuthPanel.razor`
- **Features**: Device flow code display, authorization link

#### GitHubProjectSelector

- **Purpose**: Project dropdown with search
- **Location**: `src/GexVisor.UI/Components/GitHub/GitHubProjectSelector.razor`
- **Features**: Lists user's projects, displays selected project info

#### GitHubKanbanBoard

- **Purpose**: Dynamic board based on project's status field
- **Location**: `src/GexVisor.UI/Components/GitHub/GitHubKanbanBoard.razor`
- **Features**:
  - Renders columns from status field options
  - Click-to-open items in new tab
  - State indicators (open=green, closed=purple)
  - Responsive CSS Grid layout

#### UnifiedKanbanBoard

- **Purpose**: Normalized 3-column view (Backlog/In Progress/Done)
- **Location**: `src/GexVisor.UI/Components/GitHub/UnifiedKanbanBoard.razor`
- **Features**:
  - StatusMapper integration
  - Shows original status badge on cards
  - Confidence indicators (? for inferred, ⚙️ for custom)
  - Mapping override panel

## Usage

### Viewing GitHub Projects

1. **Authenticate** with GitHub OAuth
2. **Select a project** from the dropdown
3. **Switch tabs**:
   - "Local Tasks" - Your local task board
   - "GitHub: {ProjectName}" - Dynamic view matching GitHub's columns
   - "Unified View" - Normalized 3-column Kanban

### Status Mapping

The Unified View automatically maps GitHub statuses to standard columns:

| Standard Column | Example GitHub Statuses |
|----------------|------------------------|
| **Backlog** | Todo, Not Started, Open, Pending, Waiting, Queued |
| **In Progress** | In Progress, Doing, Active, Working, In Review, WIP |
| **Done** | Done, Completed, Closed, Resolved, Fixed, Shipped |

**Custom Mappings**: If a status is incorrectly mapped, use the mapping panel to override it.
Custom mappings are saved per-project and persist across sessions.

## API Details

### GitHub GraphQL Queries

The integration uses GraphQL queries for all GitHub API interactions:

```graphql
query GetProjects {
  viewer {
    projectsV2(first: 20) {
      nodes {
        id
        title
        url
        # ... fields
      }
    }
  }
}

query GetProjectItems($projectId: ID!) {
  node(id: $projectId) {
    ... on ProjectV2 {
      items(first: 100) {
        nodes {
          id
          content {
            ... on Issue {
              title
              url
              state
            }
          }
          fieldValues {
            # ... custom fields
          }
        }
      }
    }
  }
}
```

## Pagination

GexVisor implements cursor-based pagination for GitHub Projects using GraphQL's `pageInfo` mechanism.

### Implementation

**Query Structure:**

```graphql
query GetProjectItems($projectId: ID!, $after: String) {
  node(id: $projectId) {
    ... on ProjectV2 {
      items(first: 100, after: $after) {
        pageInfo {
          hasNextPage
          endCursor
        }
        nodes {
          # ... item fields
        }
      }
    }
  }
}
```

**Key Features:**

- Initial load fetches first 100 items
- `pageInfo.endCursor` stored in `BoardStateService`
- "Load More Projects" button calls `FetchMoreProjectsAsync(cursor)`
- Deduplicates items by ID when appending
- Pagination state persists to localStorage

**UI Component:**
`src/GexVisor.UI/Components/GitHubProjectSelector.razor` (lines 85-95)

- Shows "Load More" button when `pageInfo.hasNextPage == true`
- Loading state prevents duplicate requests
- Automatically scrolls to newly loaded items

**Service Methods:**

- `BoardStateService.FetchProjectItemsAsync()` - Initial load
- `BoardStateService.FetchMoreProjectsAsync(cursor)` - Paginated fetch

### Testing

Test with projects containing >100 items to verify pagination flow.

---

### Rate Limits

- **Primary Rate Limit**: 5,000 points per hour
- **Secondary Rate Limit**: Avoid rapid-fire requests
- **Caching**: BoardStateService uses 5-minute cache to minimize API calls

### CORS Support

GitHub's GraphQL API supports browser-based requests with proper authentication headers.
No proxy server is required for the client-side integration.

## Data Persistence

### localStorage Schema

```json
{
  "gexvisor.github.token": "gho_xxxxxxxxxxxx",
  "gexvisor.github.selectedProject": {
    "id": "PVT_xxxxxxxxxxxx",
    "title": "My Research Project",
    "url": "https://github.com/users/username/projects/1"
  },
  "gexvisor.statusMappings": {
    "projectMappings": {
      "PVT_xxxxxxxxxxxx": {
        "In Dev": "In Progress",
        "Ready for Review": "In Progress"
      }
    }
  }
}
```

## Security Considerations

### Token Storage

- **Tokens stored in localStorage** (browser storage)
- **Scoped permissions**: Only `project` and `repo` access
- **No server-side storage**: Fully client-side architecture
- **User controls**: Clear tokens via "Disconnect" button

### Best Practices

1. **Never commit tokens** to version control
2. **Revoke tokens** when no longer needed (GitHub Settings → Developer Settings → Tokens)
3. **Use device flow** for secure authentication on untrusted devices
4. **Audit token usage** regularly in GitHub Settings

## Troubleshooting

### "Authentication Failed"

- Ensure you copied the full device code
- Check that you authorized within the 15-minute window
- Verify your GitHub account has access to Projects v2

### "No Projects Found"

- Create a GitHub Project v2 in your account or organization
- Ensure the project has at least one status field
- Refresh the page and re-authenticate

### "Items Not Loading"

- Check browser console for GraphQL errors
- Verify the project has items (issues/PRs)
- Try refreshing the board with the refresh button

### Status Mapping Issues

- Use the mapping panel in Unified View to override incorrect mappings
- Ensure your GitHub status names match the inference patterns
- Add custom mappings for organization-specific status names

## Development

### Adding New Status Patterns

Edit `StatusMapper.cs` to add new inference patterns:

```csharp
private static readonly Dictionary<string, string[]> DefaultInferenceRules = new()
{
    [NormalizedStatus.InProgress] = [
        "in progress", "doing", "active",
        "your-custom-status" // Add here
    ],
};
```

### Testing GitHub Integration

1. **Unit Tests**: Mock GitHubProjectService responses
2. **Integration Tests**: Use GitHub's GraphQL Explorer for schema validation
3. **Manual Testing**: Create test projects with various status configurations

### Extending Functionality

To add write-back support (currently read-only):

1. Add mutation queries to GitHubProjectService
2. Implement status update methods
3. Handle optimistic UI updates
4. Add conflict resolution logic

## Related Issues

- #69 - GitHub Projects Integration (epic)
- #70 - GitHubProjectService implementation
- #71 - GitHub Auth UI
- #72 - Project Selector Component
- #73 - Sync TaskBoard with GitHub Projects
- #74 - GitHubKanbanBoard dynamic columns
- #75 - UnifiedKanbanBoard with StatusMapper

## Resources

- [GitHub Projects v2 Documentation](https://docs.github.com/en/issues/planning-and-tracking-with-projects)
- [GitHub GraphQL API](https://docs.github.com/en/graphql)
- [GitHub OAuth Device Flow](https://docs.github.com/en/developers/apps/building-oauth-apps/authorizing-oauth-apps#device-flow)
- [Personal Access Tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)

---

_Last Updated: 2026-01-24_
