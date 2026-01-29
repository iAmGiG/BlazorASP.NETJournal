# Radar Component Rendering Troubleshooting Guide

## Overview

This document describes radar component rendering issues discovered on 2026-01-29 and their resolution. It covers the root causes, diagnostic procedures, and preventive measures.

## Issue Summary

After merging 58 commits from `origin/development` on 2026-01-29, the InteractiveRadar and MiniRadar components appeared completely blank/black with no SVG nodes rendering in the DOM.

**Affected components:**
- `MiniRadar.razor` (home page)
- `InteractiveRadar.razor` (/arcade and /research/complexity pages)

**Impact:** Users saw blank radar containers instead of the colored node-based visualization.

**Root cause:** Data loading was failing silently, returning empty arrays.

---

## Root Cause Analysis

### Issue 1: Silent Data Loading Failures

**Component:** `ResearchPathService.cs`

The service was swallowing all exceptions and errors, returning empty arrays `[]` without any user or developer feedback:

```csharp
// BEFORE (problematic)
try {
    var response = await _httpClient.GetAsync("data/research-paths.json");
    if (!response.IsSuccessStatusCode) {
        Console.WriteLine("Could not load research paths: HTTP error");
        return [];  // Silent failure
    }
    // ... deserialization ...
}
catch (Exception ex) {
    Console.WriteLine($"Could not load research paths: {ex.Message}");
    return [];  // Silent failure
}
```

**Problems:**
- HTTP 404 errors were silently ignored
- JSON deserialization failures were caught but not logged with details
- Components had no way to distinguish between "loading" and "empty data"
- Error messages were too generic to diagnose the actual problem

### Issue 2: No Component Loading States

**Components:** `MiniRadar.razor`, `InteractiveRadar.razor`

Components rendered nothing when data was missing:

```csharp
// BEFORE (no loading state)
protected override async Task OnInitializedAsync() {
    _paths = await ResearchPathService.LoadPathsAsync();
    // If this returns [], component renders nothing with no feedback
}

@foreach (var path in _paths) {
    <g class="research-node">...</g>
    // No paths = no SVG elements in DOM
}
```

**Problems:**
- Users saw blank screens with no explanation
- No loading indicator appeared
- No error message informed users of failures
- Impossible to debug from user perspective

### Issue 3: Initial SVG/CSS Fixes Were Incomplete

Earlier investigation identified and fixed 3 CSS/SVG issues:
1. ✓ Transform hierarchy bug (commit fdedb3e) - FIXED
2. ✓ Overflow clipping conflict (commit 47ce3e5) - FIXED
3. ✓ Focus mode opacity conflict (commit 5daf4c7) - FIXED

However, these fixes only affected *how* nodes would render if data was available. They didn't address the core problem: *no data was loading at all*.

---

## Diagnostic Procedure

### Step 1: Check Browser Console

Open DevTools Console (F12) and look for output from `ResearchPathService`:

```
📡 ResearchPathService: Attempting to load data/research-paths.json
   HttpClient BaseAddress: https://localhost:5001/
   HTTP Response: 200
✓ ResearchPathService: Received 15234 bytes
   First 100 chars: { "paths": [ { "id": "core", ...
✓ ResearchPathService: Successfully loaded 42 research paths
   First path ID: core
```

### Step 2: Identify Failure Points

**Scenario A: HTTP 404**
```
❌ ResearchPathService: HTTP 404 for data/research-paths.json
   Full URL attempted: https://localhost:5001/data/research-paths.json
```
→ Solution: Verify file exists in `wwwroot/data/research-paths.json`

**Scenario B: Deserialization Failure**
```
❌ ResearchPathService EXCEPTION: JsonException
   Message: The JSON value could not be converted to ResearchPath[]
   Stack trace: ...
```
→ Solution: Check JSON structure matches C# model, verify property names and enum values

**Scenario C: Silent Success with Empty Array**
```
✓ ResearchPathService: Successfully loaded 42 research paths
   First path ID: core
⚠️ MiniRadar: ResearchPathService returned empty array
```
→ Solution: Investigate why wrapper deserialized but paths array is empty

**Scenario D: No Console Output**
```
(silence)
```
→ Solution: Check if ResearchPathService is registered in DI container, verify component is being rendered

---

## Solutions Implemented

### Fix 1: Enhanced Logging in ResearchPathService

**File:** `src/GexVisor.UI/Services/ResearchPathService.cs:35-82`

Added comprehensive logging at every step:

```csharp
public async Task<ResearchPath[]> LoadPathsAsync()
{
    if (_paths != null) {
        return _paths;
    }

    try {
        Console.WriteLine("📡 ResearchPathService: Attempting to load data/research-paths.json");
        Console.WriteLine($"   HttpClient BaseAddress: {_httpClient.BaseAddress}");

        var response = await _httpClient.GetAsync("data/research-paths.json");
        Console.WriteLine($"   HTTP Response: {response.StatusCode}");

        if (!response.IsSuccessStatusCode) {
            Console.WriteLine($"❌ ResearchPathService: HTTP {response.StatusCode}");
            Console.WriteLine($"   Full URL attempted: {response.RequestMessage?.RequestUri}");
            return [];
        }

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"✓ ResearchPathService: Received {json.Length} bytes");
        Console.WriteLine($"   First 100 chars: {json.Substring(0, Math.Min(100, json.Length))}");

        var wrapper = JsonSerializer.Deserialize<ResearchPathsWrapper>(json, _jsonOptions);

        if (wrapper == null) {
            Console.WriteLine("❌ ResearchPathService: Deserialization returned null wrapper");
            return [];
        }

        if (wrapper.Paths == null || wrapper.Paths.Length == 0) {
            Console.WriteLine("⚠️ ResearchPathService: Paths array is null or empty");
            Console.WriteLine($"   Wrapper type: {wrapper.GetType().FullName}");
        } else {
            Console.WriteLine($"✓ ResearchPathService: Successfully loaded {wrapper.Paths.Length} research paths");
            Console.WriteLine($"   First path ID: {wrapper.Paths[0].Id}");
        }

        _paths = wrapper.Paths ?? [];
        return _paths;
    }
    catch (Exception ex) {
        Console.WriteLine($"❌ ResearchPathService EXCEPTION: {ex.GetType().Name}");
        Console.WriteLine($"   Message: {ex.Message}");
        Console.WriteLine($"   Stack trace:");
        Console.WriteLine(ex.StackTrace);
        return [];
    }
}
```

**Benefits:**
- Clear identification of failure points
- Shows exact HTTP status codes and URLs
- Displays JSON content received (first 100 chars for debugging)
- Shows wrapper and array details
- Complete exception information including stack traces

### Fix 2: Add Loading States to Components (Recommended)

**File:** `src/GexVisor.UI/Components/Research/MiniRadar.razor:87-93`

Add state tracking:

```csharp
@code {
    private ResearchPath[] _paths = [];
    private bool _isLoading = true;
    private string? _errorMessage = null;

    protected override async Task OnInitializedAsync() {
        try {
            Console.WriteLine("MiniRadar: OnInitializedAsync started");
            _isLoading = true;
            _paths = await ResearchPathService.LoadPathsAsync();

            if (_paths.Length == 0) {
                _errorMessage = "No research paths loaded";
                Console.WriteLine("⚠️ MiniRadar: ResearchPathService returned empty array");
            } else {
                Console.WriteLine($"✓ MiniRadar: Loaded {_paths.Length} paths");
            }
        }
        catch (Exception ex) {
            _errorMessage = $"Failed to load paths: {ex.Message}";
            Console.WriteLine($"❌ MiniRadar EXCEPTION: {ex}");
        }
        finally {
            _isLoading = false;
            Console.WriteLine("MiniRadar: OnInitializedAsync completed");
        }
    }
}
```

Add to markup:

```razor
<div class="mini-radar" style="width: @(Size)px; height: @(Size)px;">
    @if (_isLoading) {
        <div class="radar-loading" style="display: flex; align-items: center; justify-content: center; height: 100%; color: rgba(255,255,255,0.6);">
            <p>Loading research paths...</p>
        </div>
    }
    else if (_errorMessage != null) {
        <div class="radar-error" style="display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; color: #ff4444; padding: 20px; text-align: center;">
            <p style="margin: 0 0 10px 0;">⚠️ Error Loading Radar</p>
            <p style="margin: 0; font-size: 0.9em; opacity: 0.8;">@_errorMessage</p>
            <p style="margin: 10px 0 0 0; font-size: 0.85em; opacity: 0.6;">Check browser console for details</p>
        </div>
    }
    else if (_paths.Length == 0) {
        <div class="radar-empty" style="display: flex; align-items: center; justify-content: center; height: 100%; color: rgba(255,255,255,0.4);">
            <p>No research paths available</p>
        </div>
    }
    else {
        <!-- Existing SVG rendering code -->
```

Similar updates needed for `InteractiveRadar.razor` (see plan file for details).

---

## Testing the Fix

### Phase 1: Verify Logging

1. Run the application: `dotnet run --project src/GexVisor.UI`
2. Open browser to https://localhost:5001 (or displayed port)
3. Open DevTools Console (F12)
4. Navigate to `/arcade` or `/research/complexity`
5. Look for `ResearchPathService` logging output

### Phase 2: Identify Actual Issue

Based on console output, diagnose:
- Is HTTP 404 being returned?
- Is JSON being received but deserialization failing?
- Is data loading but components not rendering?

### Phase 3: Fix Root Cause

Once identified, apply appropriate fix:
- **HTTP 404:** Verify `wwwroot/data/research-paths.json` exists
- **Deserialization failure:** Check JSON structure against `ResearchPath` model
- **No component re-render:** Ensure component is being displayed on page

### Phase 4: Run Tests

```bash
dotnet test --no-build
```

Verify all 408 tests still pass (35 Core + 277 UI + 96 API)

---

## Prevention Measures

### 1. Always Add Loading States

Every component that loads async data should show:
- Loading indicator while fetching
- Error message if load fails
- Empty state message if result is empty
- Content when load succeeds

### 2. Comprehensive Error Logging

All service methods that could fail should log:
- When operation starts
- HTTP status codes and URLs
- Received data (at least first 100 chars)
- Detailed exception information

### 3. Unit Tests for Data Loading

Create tests that verify:
- Service returns correct data on success
- Service handles 404 errors gracefully
- Service handles deserialization failures
- Components display loading states correctly
- Components display error messages correctly

### 4. Visual Tests for SVG Rendering

When updating SVG/CSS:
- Test on multiple screen sizes (480px, 768px, 1024px+)
- Verify node animations complete (opacity 0→1)
- Test zoom/pan operations don't clip nodes
- Test focus mode animations don't overlap

---

## Related Commits

- `fdedb3e` (Jan 26, 2026) - "refactor: Improve InteractiveRadar reset button UX" - Transform hierarchy fix
- `47ce3e5` (Jan 25, 2026) - "feat(radar): Add zoom/pan and navigation history" - Overflow clipping issue
- `5daf4c7` (Jan 27, 2026) - "feat: Add InteractiveRadar Focus Mode" - Opacity !important conflict

---

## Files Modified

- `src/GexVisor.UI/Services/ResearchPathService.cs` - Enhanced logging
- `src/GexVisor.UI/Components/Research/MiniRadar.razor` - Loading states (optional but recommended)
- `src/GexVisor.UI/Components/Research/InteractiveRadar.razor` - Loading states (optional but recommended)

---

## Additional Resources

- See `docs/radar-rendering-troubleshooting.md` (this file) for troubleshooting
- See `src/GexVisor.UI/Components/Research/InteractiveRadar.razor.css` for CSS animations
- See `src/GexVisor.UI/Models/ResearchPath.cs` for data model definition
- See `docs/research-visualization.md` for component architecture overview

---

## Summary

The radar rendering issue was caused by silent data loading failures. By adding comprehensive logging and optional loading states, the system now provides clear feedback about:
1. Whether data is loading
2. Why loading failed (HTTP status, deserialization error, etc.)
3. What the user should do (check console, wait, etc.)

This makes the system more maintainable and easier to debug in the future.
