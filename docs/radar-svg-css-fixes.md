# Radar SVG/CSS Rendering Fixes

## Overview

This document describes three critical SVG/CSS rendering bugs discovered in the InteractiveRadar component after merging commits on 2026-01-29. All three have been fixed.

---

## Issue 1: Transform Stacking Context Bug

**Severity:** HIGH
**Introduced in:** Commit `fdedb3e` (Jan 26, 2026) - "refactor: Improve InteractiveRadar reset button UX"
**File:** `src/GexVisor.UI/Components/Research/InteractiveRadar.razor:128-156`
**Status:** ✓ FIXED

### Problem

A nested `<g>` element wrapper was added that separated the `transform="translate()"` attribute from the `.research-node` CSS class. This broke CSS animation composition:

```razor
<!-- BROKEN: nested structure -->
<g transform="translate(@F(x), @F(y))" role="button">
    <g class="research-node node-ring-@path.Ring @(...)">
        <circle cx="0" cy="0" r="@radius" fill="@statusColor" class="node-circle" />
    </g>
</g>
```

**Why it's broken:**
- CSS animation applies `transform: scale()` to `.research-node` (inner `<g>`)
- Position `translate()` is on outer `<g>`
- Creates separate stacking contexts and transform composition hierarchy
- Entrance animations don't position correctly relative to viewport
- Hover effects may not scale around intended origin

### Solution

Removed the nested wrapper and consolidated attributes to single `<g>` element:

```razor
<!-- FIXED: single element with all attributes -->
<g class="research-node node-ring-@path.Ring @(isSelected ? "selected" : "") @(isDimmed ? "dimmed" : "") @(isRelated ? "related-glow" : "")"
   transform="translate(@F(x), @F(y))"
   role="button"
   tabindex="-1"
   aria-label="@path.Title"
   aria-describedby="desc-@path.Id"
   @onclick="@(() => HandleNodeClick(path))"
   @onclick:stopPropagation="true"
   @onmouseenter="@(() => HandleNodeHover(path))"
   @onmouseleave="@(() => HandleNodeHover(null))"
   @ontouchstart="@((e) => HandleTouchStart(e, path))"
   @ontouchend="@((e) => HandleTouchEnd(e, path))"
   @ontouchmove="@HandleTouchMove">
    <!-- Children here -->
</g>
```

**Benefits:**
- CSS transforms compose correctly
- Entrance animations position properly
- Hover scaling works around correct origin (node center)
- Simpler DOM tree

### Related CSS

The CSS animations rely on proper transform composition:

```css
@keyframes nodeEntrance {
    0% {
        opacity: 0;
        transform: scale(0.3);  /* Scales from 30% to 100% */
    }
    100% {
        opacity: 1;
        transform: scale(1);
    }
}

::deep .research-node {
    cursor: pointer;
    opacity: 0;  /* Starts invisible */
    transform-origin: center;  /* Scales from center */
}

::deep .research-node.node-ring-0 {
    animation: nodeEntrance 0.5s ease-out forwards;
}

/* Ring 1-5 have increasing delays for staggered effect */
::deep .research-node.node-ring-1 {
    animation: nodeEntrance 0.5s ease-out 0.08s forwards;
}
/* ... etc for rings 2-5 ... */
```

---

## Issue 2: Overflow Clipping Conflict

**Severity:** MEDIUM-HIGH
**Introduced in:** Commit `47ce3e5` (Jan 25, 2026) - "feat(radar): Add zoom/pan and navigation history"
**File:** `src/GexVisor.UI/Components/Research/InteractiveRadar.razor.css:10 and :61`
**Status:** ✓ FIXED

### Problem

Contradictory overflow CSS settings caused nodes to be clipped during zoom operations:

```css
/* Line 10: Container clips content */
.interactive-radar {
    overflow: hidden;  /* ← Clips everything beyond container */
}

/* Line 61: SVG wants to extend beyond container */
.radar-svg {
    overflow: visible;  /* ← Conflicts with container */
}
```

**Why it's broken:**
- Container has `overflow: hidden` which clips all children
- SVG has `overflow: visible` but is inside clipping container
- When zoomed in, nodes near edges get clipped
- User can pan off-screen without visual context
- Violates principle of least surprise (one setting wins)

### Solution

Changed container to `overflow: visible` to allow zoomed content to extend:

```css
/* FIXED: Container allows overflow */
.interactive-radar {
    display: flex;
    align-items: center;
    justify-content: center;
    outline: none;
    position: relative;
    overflow: visible;  /* ← Allows zoomed nodes to extend beyond container */
}

/* SVG can now extend beyond bounds */
.radar-svg {
    width: 100%;
    height: 100%;
    overflow: visible;
}
```

**Benefits:**
- Zoomed nodes remain visible at edges
- Users can see context when panning
- Consistent behavior between container and SVG
- Natural zoom/pan experience

### Trade-off

Content can extend beyond container bounds. This is intentional - users expect zoom to allow "looking at" content beyond viewport bounds, like in maps (Google Maps, OSM, etc.).

If clipping is needed for specific use case, add `clip-path` or `clip` attribute to SVG element instead of using overflow.

---

## Issue 3: Focus Mode Opacity Conflict

**Severity:** MEDIUM
**Introduced in:** Commit `5daf4c7` (Jan 27, 2026) - "feat: Add InteractiveRadar Focus Mode"
**File:** `src/GexVisor.UI/Components/Research/InteractiveRadar.razor.css:234-237`
**Status:** ✓ FIXED

### Problem

The `!important` flag on dimmed node styles prevented entrance animations from completing:

```css
/* BROKEN: !important overrides animation */
::deep .research-node.dimmed {
    opacity: 0.15 !important;  /* ← Forces opacity even during animation */
    pointer-events: none;
}
```

**Why it's broken:**
- Entrance animation animates `opacity: 0 → 1`
- If node is dimmed, `!important` forces `opacity: 0.15`
- Animation can't override `!important` declaration
- Nodes appear instantly at 0.15 opacity instead of animating smoothly
- CSS cascade broken by forcing priority

**How Focus Mode works:**
1. User clicks a research path (node)
2. Component enters "focus mode"
3. Related nodes get `class="related-glow"`
4. Unrelated nodes get `class="dimmed"`
5. Dimmed nodes should fade to 0.15 opacity after entrance animation completes

**Problem timeline:**
1. Component renders with initial state
2. Entrance animation starts: `opacity: 0 → 1`
3. User immediately enters focus mode
4. Node gets `class="dimmed"` with `opacity: 0.15 !important`
5. Animation is interrupted and can't continue
6. Node jumps to 0.15 opacity (jarring visual)

### Solution

Removed `!important` to restore CSS cascade:

```css
/* FIXED: Removed !important to allow cascade */
::deep .research-node.dimmed {
    opacity: 0.15;  /* Can now be overridden by animations */
    pointer-events: none;
}
```

**How it works now:**
1. Entrance animation runs: `opacity: 0 → 1` (completes)
2. Node reaches full `opacity: 1`
3. User enters focus mode
4. Node gets `class="dimmed"` with `opacity: 0.15`
5. Since animation is finished, dimmed opacity takes effect
6. Node smoothly transitions to 0.15 opacity (natural)

**Benefits:**
- Entrance animations complete smoothly
- Dimming effect applies gracefully after animation
- Proper CSS cascade behavior
- No jarring opacity changes
- User experience is smooth and predictable

### CSS Specificity Analysis

**Before fix:**
```
Animation opacity (from keyframe): specificity 0-1-0 (element + animation)
Dimmed state opacity: specificity 0-1-0 with !important

Winner: !important (forces priority regardless of specificity)
```

**After fix:**
```
Animation opacity (from keyframe): specificity 0-1-0 (element + animation)
Dimmed state opacity: specificity 0-1-0 (normal declaration)

Winner: Animation completes first, then dimmed state applies when animation ends
```

---

## Related CSS Selectors

All three fixes involve the `.research-node` CSS class and related selectors:

```css
/* Base node styling */
::deep .research-node {
    cursor: pointer;
    opacity: 0;  /* Starts invisible for entrance animation */
    transform-origin: center;
}

/* Entrance animations with staggered timing */
::deep .research-node.node-ring-0 {
    animation: nodeEntrance 0.5s ease-out forwards;
}

::deep .research-node.node-ring-1 {
    animation: nodeEntrance 0.5s ease-out 0.08s forwards;
}

/* ... rings 2-5 with increasing delays ... */

/* Hover effect for interactivity */
::deep .research-node:hover {
    filter: brightness(1.2) drop-shadow(0 0 8px currentColor);
}

::deep .research-node:focus,
::deep .research-node.focused {
    filter: brightness(1.3) drop-shadow(0 0 12px currentColor);
}

/* Selected state */
::deep .research-node.selected {
    filter: brightness(1.3) drop-shadow(0 0 12px currentColor);
}

/* Related nodes in focus mode */
::deep .research-node.related-glow {
    filter: brightness(1.2) drop-shadow(0 0 8px currentColor);
}

/* Dimmed nodes (correct after fix) */
::deep .research-node.dimmed {
    opacity: 0.15;  /* No !important anymore */
    pointer-events: none;
}
```

---

## Testing SVG/CSS Fixes

### Test 1: Node Entrance Animation

1. Navigate to `/arcade` or `/research/complexity`
2. Observe initial render
3. Nodes should fade in smoothly (0 → 1 opacity) over 0.5 seconds
4. Rings render in staggered order (core first, then rings 1-5)
5. No nodes should appear instantly or jerkily

**Expected behavior:**
```
t=0.0s:   Ring 0 node starts fading in
t=0.08s:  Ring 1 node starts fading in
t=0.16s:  Ring 2 node starts fading in
...
t=0.4s:   Ring 5 node starts fading in
t=0.5s:   Ring 0 node fully visible
t=0.58s:  Ring 1 node fully visible
...
t=0.9s:   Ring 5 node fully visible
```

### Test 2: Zoom/Pan Without Clipping

1. Navigate to `/arcade`
2. Use zoom/pan controls (if implemented) or mouse wheel
3. Zoom in 2-3 times
4. Pan around the zoomed radar
5. Nodes should remain visible near edges
6. No nodes should be clipped by container

**What to avoid:**
- Nodes disappearing at edges during pan
- Partial node visibility (cut off)
- Sudden clipping when zooming

### Test 3: Focus Mode Animations

1. Navigate to `/arcade` or `/research/complexity`
2. Wait for nodes to fully render
3. Click on a central node (Ring 0 if available)
4. Component enters focus mode
5. Related nodes should remain bright
6. Unrelated nodes should dim smoothly to 0.15 opacity
7. Diming should happen without animation flicker

**What to avoid:**
- Nodes jumping to 0.15 opacity
- Unsmooth dimming transition
- Nodes becoming invisible
- Related nodes becoming dimmed

---

## Browser Compatibility

These fixes use standard CSS and SVG features:
- `transform` property - all modern browsers
- `opacity` property - all modern browsers
- `animation` with keyframes - all modern browsers (IE 10+)
- `overflow: visible/hidden` - all browsers

**Tested on:**
- Chrome/Chromium 120+
- Edge 120+
- Firefox 121+

---

## Performance Considerations

### Grid Background Optimization

The grid background in InteractiveRadar was using an oversized rectangle:

```razor
<!-- BEFORE: 4000×4000px grid (16M pixels) -->
<rect x="-2000" y="-2000" width="4000" height="4000" fill="url(#radar-grid)" class="grid-bg" />

<!-- AFTER: 800×800px grid (640k pixels) -->
<rect x="-400" y="-400" width="800" height="800" fill="url(#radar-grid)" class="grid-bg" />
```

**Benefits:**
- 5× fewer pixels to render
- Covers viewBox (-350 to +350) with 50px margin
- Better performance on lower-end devices
- No visual difference for user

---

## Summary of Fixes

| Issue | Root Cause | Fix | Benefit |
|-------|-----------|-----|---------|
| Transform hierarchy | Nested `<g>` separated transform from class | Consolidated to single element | Proper CSS animation composition |
| Overflow clipping | Contradictory `overflow` settings | Changed container to `visible` | Zoomed nodes visible at edges |
| Opacity conflict | `!important` override | Removed `!important` flag | Smooth entrance animations with dimming |

All three fixes are now in place and tested.

---

## Related Files

- `src/GexVisor.UI/Components/Research/InteractiveRadar.razor` - Component markup
- `src/GexVisor.UI/Components/Research/InteractiveRadar.razor.css` - Animations and styles
- `src/GexVisor.UI/Components/Research/MiniRadar.razor` - Smaller radar variant
- `docs/research-visualization.md` - Component architecture
- `docs/radar-rendering-troubleshooting.md` - Data loading issues

---

## Future Improvements

1. **Add unit tests for CSS animations** - Test animation classes and timing
2. **Add visual regression tests** - Screenshot comparisons for animation states
3. **Document animation timing** - CSS variables for configurable durations
4. **Mobile responsive improvements** - Better radar behavior on small screens
5. **Performance profiling** - Monitor animation frame rates on various devices

---

## Conclusion

These three SVG/CSS fixes address layout, clipping, and animation issues in the InteractiveRadar component. Combined with proper data loading (see `radar-rendering-troubleshooting.md`), the radar now renders correctly with smooth animations and proper zoom/pan behavior.
