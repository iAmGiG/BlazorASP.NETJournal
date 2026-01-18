# Issue Backlog

Issues identified during repository review. Organized by component
with priority levels.

**Priority Legend:** 🔴 Critical | 🟠 High | 🟡 Medium | 🔵 Low

---

## Core Models & Logic

### ✅ RESOLVED: Fix `JournalFramework.Id` property (static to instance)

**Labels:** `bug`, `core`, `blocking`

**File:** `src/GexVisor.Core/JournalFramework.cs:33`

~~The `Id` property is static, causing all instances to share the same ID.~~
**FIXED:** Changed to instance property in commit `dfbec18`.

**Acceptance Criteria:**

- [x] Change `public static Guid Id` to `public Guid Id`
- [x] Verify each `JournalFramework` instance has unique ID
- [x] Update any code that assumes static ID access

---

### ✅ RESOLVED: Fix `SaveTasks()` undefined `MyTasks` reference

**Labels:** `bug`, `core`, `blocking`

**File:** `src/GexVisor.Core/JournalFramework.cs` (removed), `src/GexVisor.UI/Services/TaskPersistenceService.cs` (new)

~~The `SaveTasks()` method references undefined `MyTasks` variable.~~
**FIXED:** Refactored to `TaskPersistenceService` with dependency injection in commit `dfbec18`.

**Acceptance Criteria:**

- [x] Resolve undefined `MyTasks` reference
- [x] Implement proper serialization (pass parameter or refactor to instance method)
- [x] Code compiles without errors

---

### 🟠 HIGH: Consolidate DailyLog model with JournalFramework

**Labels:** `refactor`, `core`

**File:** `BlazorJournalApp/Pages/DailyLog.razor:49-63`

The local `MyLog` class duplicates `JournalFramework` functionality
instead of extending it.

**Acceptance Criteria:**

- [ ] Remove duplicate `MyLog` class
- [ ] Have `DailyLog` page use `JournalFramework` or a derived class
- [ ] Ensure no functionality is lost

---

### ✅ RESOLVED: Implement `TradeResult()` calculation method

**Labels:** `enhancement`, `core`

**File:** `src/GexVisor.Core/JournalFramework.cs:89-97` (TradeLog), `:115-127` (OptionsLog)

~~The `TradeResult()` method is stubbed.~~
**FIXED:** Replaced with instance methods `CalculatePnL()` for both `TradeLog` and `OptionsLog`.

**Acceptance Criteria:**

- [x] Implement trade P/L calculation logic
- [x] Handle gains and losses correctly (Long/Short for stocks, BTO/STO for options)
- [ ] Add unit tests for calculation accuracy

---

## UI Components & Pages

### 🔴 CRITICAL: Fix ToDoForm binding issues

**Labels:** `bug`, `ui`, `blocking`

**File:** `BlazorJournalApp/Pages/ToDoForm.razor`

Multiple critical issues preventing ToDoForm from working:

1. References non-existent `Tasks` class (should be `ToDoTask`)
2. `SaveTasks()` method is called but not defined in `@code`

**Acceptance Criteria:**

- [ ] Import and use correct `ToDoTask` type from `GexVisor.Core`
- [ ] Implement `SaveTasks()` method in `@code` block
- [ ] Page renders without errors
- [ ] Save functionality works end-to-end

---

### 🔴 CRITICAL: Fix DailyLog invalid @bind directive

**Labels:** `bug`, `ui`, `blocking`

**File:** `BlazorJournalApp/Pages/DailyLog.razor:35`

The `@bind` directive cannot be used on `<li>` elements in Blazor.

```razor
<li @bind="LogOfTheDay[0]">  <!-- Invalid -->
```

**Acceptance Criteria:**

- [ ] Remove invalid `@bind` from `<li>`
- [ ] Use alternative binding approach (event handlers, inputs, etc.)
- [ ] Component renders without errors

---

### 🟠 HIGH: Fix CSS syntax errors

**Labels:** `bug`, `styling`

**File:** `BlazorJournalApp/wwwroot/css/app.css`

Multiple CSS syntax issues:

- Line 2: Missing semicolon after `cornflowerblue`
- Line 39: `.btn btn-primary` should be `.btn.btn-primary`
- Line 43: `btn btn-secondary` missing leading dot

**Acceptance Criteria:**

- [ ] Fix all syntax errors in app.css
- [ ] CSS validates without errors
- [ ] Styling renders correctly

---

### 🟠 HIGH: Create navigation component

**Labels:** `enhancement`, `ui`

Create a reusable `NavMenu.razor` component with links to all pages.

**Acceptance Criteria:**

- [ ] Create `BlazorJournalApp/Components/NavMenu.razor`
- [ ] Include links to: Home, Todo, Daily Log, Trade Logging, Counter
- [ ] Component is integrated into `MainLayout.razor`
- [ ] Navigation works on all pages

---

### 🟠 HIGH: Enhance MainLayout with proper structure

**Labels:** `enhancement`, `ui`

**File:** `BlazorJournalApp/MainLayout.razor`

Current layout only contains `@Body`. Add proper layout structure.

**Acceptance Criteria:**

- [ ] Add header with app title
- [ ] Integrate navigation (NavMenu component)
- [ ] Add optional footer
- [ ] Layout is responsive and visually organized

---

### 🟡 MEDIUM: Add dynamic task list to ToDoForm

**Labels:** `enhancement`, `ui`

**File:** `BlazorJournalApp/Pages/ToDoForm.razor:23-34`

Replace hardcoded 4 tasks with dynamic `@foreach` rendering.

**Acceptance Criteria:**

- [ ] Use `@foreach` to render task list from collection
- [ ] Support any number of tasks (not hardcoded to 4)
- [ ] Each task renders with proper bindings

---

### 🟡 MEDIUM: Implement add/delete task functionality (Part 1: Add)

**Labels:** `enhancement`, `ui`

**File:** `BlazorJournalApp/Pages/ToDoForm.razor`

Allow users to add new tasks via form.

**Acceptance Criteria:**

- [ ] Create form input for new task entry
- [ ] Implement "Add Task" button handler
- [ ] New task appears in task list
- [ ] Input clears after adding

---

### 🟡 MEDIUM: Implement delete task functionality (Part 2: Delete)

**Labels:** `enhancement`, `ui`

**File:** `BlazorJournalApp/Pages/ToDoForm.razor`

Allow users to delete existing tasks.

**Acceptance Criteria:**

- [ ] Add delete button to each task item
- [ ] Implement delete handler
- [ ] Task is removed from list when deleted
- [ ] UI updates immediately

---

### 🟡 MEDIUM: Implement edit task functionality (Part 3: Edit)

**Labels:** `enhancement`, `ui`

**File:** `BlazorJournalApp/Pages/ToDoForm.razor`

Allow users to edit task details.

**Acceptance Criteria:**

- [ ] Enable inline editing or edit form
- [ ] Task updates when edit is saved
- [ ] Changes persist to collection

---

### 🟡 MEDIUM: Implement TradeLogging page

**Labels:** `enhancement`, `feature`

**File:** `BlazorJournalApp/Pages/TradeLogging.razor`

The page is stubbed with commented code. Implement full trading journal UI.

**Acceptance Criteria:**

- [ ] Create form for entering trade data
- [ ] Bind form to `TradeLog` model
- [ ] Implement save functionality
- [ ] Display list of saved trades

---

## Data Persistence

### 🟠 HIGH: Implement client-side data persistence (localStorage)

**Labels:** `enhancement`, `feature`, `backend`

Add browser localStorage for client-side persistence. Phase 1 of persistence strategy.

**Acceptance Criteria:**

- [ ] Create data service for localStorage operations
- [ ] Implement save/load for tasks
- [ ] Data persists across page refreshes
- [ ] Handle localStorage quota gracefully

---

### 🟡 MEDIUM: Implement IndexedDB persistence layer (Phase 2)

**Labels:** `enhancement`, `feature`, `backend`

Add IndexedDB for larger datasets once localStorage is working.

**Acceptance Criteria:**

- [ ] Create IndexedDB wrapper/service
- [ ] Support for multiple log types
- [ ] Graceful fallback to localStorage if unavailable
- [ ] Performance is acceptable for large datasets

---

### 🔵 LOW: Plan API backend persistence (Phase 3)

**Labels:** `enhancement`, `feature`, `backend`, `discussion`

Research and plan ASP.NET backend for future persistent storage.

**Acceptance Criteria:**

- [ ] Document API design (endpoints, data models)
- [ ] Identify Entity Framework Core setup
- [ ] Create migration plan from local to API-backed storage

---

## Research Visuals & Documentation

### 🟠 HIGH: Create Research Arcade landing page

**Labels:** `enhancement`, `documentation`, `feature`

**File:** `docs/research-visuals/index.html`

Create landing page for the research visual arcade showcasing all interactive
visualizations with descriptions and quick access.

**Acceptance Criteria:**

- [ ] Create index.html with card-based layout
- [ ] Link all research visualizations
- [ ] Display key metrics (71.5% detection, 91.2% accuracy, etc.)
- [ ] Include three-paper dissertation arc overview
- [ ] Responsive design for all screen sizes

---

### 🟡 MEDIUM: Integrate Research Arcade into main app

**Labels:** `enhancement`, `documentation`, `feature`

Link research visuals from the main Blazor app navigation, allowing users
to explore visualizations directly from the application.

**Acceptance Criteria:**

- [ ] Add "Research" or "Visualizations" menu item to navigation
- [ ] Create Blazor page that embeds or links to research arcade
- [ ] Include breadcrumb navigation
- [ ] Track which visualizations are most viewed (optional)

---

### 🔵 LOW: Create research-archive subdirectory

**Labels:** `documentation`, `reference`

Set up archive for historical research notes, papers, and supplementary
materials.

**Acceptance Criteria:**

- [ ] Create `docs/research-archive/` directory structure
- [ ] Document naming conventions for archived materials
- [ ] Create README for archive access guidelines

---

## CI/CD & Development Infrastructure

### 🟠 HIGH: Set up GitHub Actions CI workflow

**Labels:** `infrastructure`, `devops`, `automation`

**File:** `.github/workflows/ci.yaml`

Implement automated continuous integration pipeline to validate code
quality on push and pull requests.

**Acceptance Criteria:**

- [ ] Create GitHub Actions workflow for CI
- [ ] Run markdown linting on documentation
- [ ] Build verification with .NET 8
- [ ] Code formatting check with dotnet format
- [ ] StyleCop analysis for C# code
- [ ] All checks pass on PR before merge

---

### 🟠 HIGH: Configure pre-commit hooks for local validation

**Labels:** `infrastructure`, `devops`, `automation`

**File:** `.pre-commit-config.yaml`

Set up pre-commit framework to validate code locally before pushing,
preventing bad commits from reaching remote.

**Acceptance Criteria:**

- [ ] Create `.pre-commit-config.yaml`
- [ ] Include markdown, YAML, and C# linting
- [ ] Hooks run automatically on `git commit`
- [ ] Document setup in CONTRIBUTING.md
- [ ] Team members install and use hooks

---

### 🟠 HIGH: Create CONTRIBUTING.md developer guide

**Labels:** `documentation`, `infrastructure`

**File:** `CONTRIBUTING.md`

Provide clear documentation for setting up development environment,
code standards, and contribution workflow.

**Acceptance Criteria:**

- [ ] Document .NET setup and dependencies
- [ ] Explain pre-commit hooks installation
- [ ] Document C# naming conventions
- [ ] Include commit message guidelines
- [ ] Explain GitHub Actions validation
- [ ] Provide testing examples

---

### 🔵 LOW: Add .editorconfig for consistent formatting

**Labels:** `infrastructure`, `configuration`

**File:** `.editorconfig`

Configure editor settings to enforce consistent code formatting
across different editors (VS Code, Visual Studio, Rider).

**Acceptance Criteria:**

- [ ] Create .editorconfig with C#, Markdown, YAML rules
- [ ] Test with VS Code and Visual Studio
- [ ] Verify IDE plugins recognize settings
- [ ] Document in CONTRIBUTING.md

---

### 🔵 LOW: Configure markdownlint rules

**Labels:** `infrastructure`, `documentation`

**File:** `.markdownlint.json`

Define markdown linting rules for documentation consistency
and quality.

**Acceptance Criteria:**

- [ ] Create .markdownlint.json with project rules
- [ ] Set line length to 80 characters
- [ ] Verify existing docs pass validation
- [ ] Integrate into CI/CD pipeline

---

## Backend & Infrastructure

### 🔵 LOW: Integrate Entity Framework Core

**Labels:** `enhancement`, `backend`, `architecture`

Set up EF Core for database persistence (future phase, after localStorage works).

**Acceptance Criteria:**

- [ ] Create DbContext with entities for Journal, Trade, Todo
- [ ] Configure entity relationships
- [ ] Create initial migrations
- [ ] Test CRUD operations

---

### 🔵 LOW: Create ASP.NET API backend project

**Labels:** `enhancement`, `backend`, `architecture`

Create separate API project for data operations.

**Acceptance Criteria:**

- [ ] Create new `BlazorJournalApp.API` project
- [ ] Implement REST endpoints for CRUD operations
- [ ] Add proper error handling
- [ ] Document API endpoints

---

### 🔵 LOW: Implement user authentication

**Labels:** `enhancement`, `security`, `architecture`

Add ASP.NET Identity for multi-user support.

**Acceptance Criteria:**

- [ ] Set up ASP.NET Identity
- [ ] Implement registration/login pages
- [ ] Associate logs with user accounts
- [ ] Secure API endpoints with authentication

---

### 🔵 LOW: Implement cross-linking between log types

**Labels:** `enhancement`, `feature`

Allow linking Journal, Trade, and Todo entries via `LinkedLogId`.

**Acceptance Criteria:**

- [ ] Add `LinkedLogId` navigation properties to models
- [ ] Create UI for linking entries
- [ ] Display linked items in detail views

---

## Polish & Maintenance

### 🔵 LOW: Fix typo in Counter component

**Labels:** `typo`, `polish`

**File:** `BlazorJournalApp/Pages/Counter.razor:14`

"Decrese" should be "Decrease"

**Acceptance Criteria:**

- [ ] Update text to "Decrease"

---

### 🔵 LOW: Remove debug text from Counter

**Labels:** `cleanup`, `polish`

**File:** `BlazorJournalApp/Pages/Counter.razor:15`

Remove "High how are you today?" text.

**Acceptance Criteria:**

- [ ] Remove debug text
- [ ] Component still functions normally

---

### 🔵 LOW: Enable PageTitle on Index page

**Labels:** `enhancement`, `ui`, `polish`

**File:** `BlazorJournalApp/Pages/Index.razor:3`

Uncomment and configure the `PageTitle` component.

**Acceptance Criteria:**

- [ ] Uncomment PageTitle
- [ ] Set meaningful title text
- [ ] Title appears in browser tab
