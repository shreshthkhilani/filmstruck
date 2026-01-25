# FilmStruck Co-op Technical Specification

## Phase 1: Companion Filtering ✓ IMPLEMENTED

### Overview

Add the ability to filter the film log by one or more companions on the web UI, showing only films watched together and recalculating stats for that intersection.

**Example use case:** s4s wants to see all films watched with Varnica, or films watched with both Sophie and Varnica together.

### Goals

- Filter poster grid by companion(s) via URL parameters
- Show intersection stats (films watched together, shared top directors, etc.)
- Support 1-2 companion filters
- Maintain CLI + local CSV + static file architecture
- No backend server required

### Non-Goals (Phase 1)

- Multi-user accounts or authentication
- Separate film logs per user
- Real-time sync between users
- Mobile app

---

## Architecture

### Current State

```
CLI → CSV files → build.js → static index.html → GitHub Pages
```

### Phase 1 Addition

```
CLI → CSV files → build.js → static index.html (with embedded data + JS) → GitHub Pages
                                    ↓
                            URL params drive client-side filtering
                                    ↓
                            ?with=Varnica or ?with=Sophie,Varnica
```

The key change: embed all log data as JSON in the HTML, then use client-side JavaScript to filter and re-render based on URL parameters.

---

## Data Model

### No Schema Changes

The existing `log.csv` already captures companion data:

```csv
date,title,location,companions,tmdbId
3/10/2024,Snowpiercer,600 Park Pl,Yash,110415
3/17/2024,Love Lies Bleeding,BAM,"Lee,Diane,Clio,Jocelyn",948549
```

A film is "watched with" a companion if that companion appears in the comma-separated `companions` field.

### Filtering Logic

**Single companion filter** (`?with=Varnica`):
- Show films where `companions` includes "Varnica"

**Two companion filter** (`?with=Sophie,Varnica`):
- Show films where `companions` includes BOTH "Sophie" AND "Varnica"
- This represents sessions where all three (s4s + Sophie + Varnica) watched together

**No filter** (default):
- Show all films (current behavior)

---

## URL Schema

### Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `with` | string | Comma-separated companion names (max 2) |

### Examples

| URL | Behavior |
|-----|----------|
| `/` | All films, all stats |
| `/?with=Varnica` | Films watched with Varnica |
| `/?with=Sophie,Varnica` | Films watched with both Sophie AND Varnica |

### URL Encoding

Companion names with special characters should be URL-encoded:
- `?with=Mary%20Jane` for "Mary Jane"

---

## Implementation

### 1. Modify `build.js`

Embed log and film data as JSON in the generated HTML:

```javascript
const html = `<!DOCTYPE html>
<html>
<head>...</head>
<body>
  <div id="app"></div>
  <script>
    const LOG_DATA = ${JSON.stringify(watchedFilms)};
    const COMPANIONS_LIST = ${JSON.stringify(allCompanions)};
    // ... filtering and rendering logic
  </script>
</body>
</html>`;
```

### 2. Client-Side JavaScript

Add to generated HTML:

```javascript
// Parse URL parameters
function getCompanionFilter() {
  const params = new URLSearchParams(window.location.search);
  const withParam = params.get('with');
  if (!withParam) return [];
  return withParam.split(',').map(c => c.trim()).slice(0, 2);
}

// Filter films by companions
function filterFilms(films, companions) {
  if (companions.length === 0) return films;
  return films.filter(f => {
    const filmCompanions = f.companions
      .split(',')
      .map(c => c.trim())
      .filter(c => c);
    return companions.every(c => filmCompanions.includes(c));
  });
}

// Calculate stats for filtered films
function calculateStats(films) {
  // ... aggregate by director, language, decade, etc.
}

// Render poster grid and stats
function render(films, stats, filterLabel) {
  // ... update DOM
}

// Main
const filter = getCompanionFilter();
const filtered = filterFilms(LOG_DATA, filter);
const stats = calculateStats(filtered);
const label = filter.length > 0 ? `with ${filter.join(' & ')}` : 's4s';
render(filtered, stats, label);
```

### 3. UI Changes

#### Header

Show active filter in header:

```
filmstruck
s4s                     (no filter)
s4s with Varnica        (single companion)
s4s with Sophie & Varnica   (two companions)
```

#### Companion Selector

Add a simple companion picker below the header:

```
[Filter by companion: ▼]

Dropdown options:
- (none) - show all
- Varnica (12 films)
- Sophie (8 films)
- Yash (5 films)
- ...
```

When a companion is selected, update URL and re-render. For two companions, show a second dropdown.

#### Stats Footer

Stats recalculate for filtered view:
- "12 films" (filtered count)
- Top directors, languages, decades (for filtered set)
- "top companions" section hidden when filtering (or shows "other companions" in those sessions)

### 4. File Changes

| File | Changes |
|------|---------|
| `build.js` | Embed JSON data, generate companion list, add client-side JS |
| `index.html` | (generated) Now includes filtering logic |

No changes to:
- CLI commands
- CSV schema
- Stats calculation in CLI (still useful for full stats)

---

## Edge Cases

### Empty Results

If filter yields no films:
- Show message: "No films watched with {companion(s)} yet"
- Show empty stats

### Unknown Companion

If URL contains companion not in data:
- Treat as valid filter, will return empty results
- Don't error

### Case Sensitivity

Companion matching should be case-insensitive:
- `?with=varnica` matches "Varnica" in data

### Special Characters in Names

- URL-encode names with spaces/special chars
- Decode before matching

---

## Future Phases (Out of Scope)

### Phase 2: Multi-User Logs
- Each user has their own log.csv
- Directory structure: `data/users/{username}/log.csv`
- Cross-user intersection: "films both s4s and varnica have watched"

### Phase 3: Collaborative Features
- Shared watchlists
- Film recommendations based on intersection
- "Watch together" suggestions

### Phase 4: User Accounts
- Authentication
- Personal settings
- Privacy controls

---

## Testing

### Manual Test Cases

1. **No filter**: Visit `/` - shows all films, full stats
2. **Single companion**: Visit `/?with=Yash` - shows only films with Yash
3. **Two companions**: Visit `/?with=Lee,Diane` - shows films with both
4. **Unknown companion**: Visit `/?with=Unknown` - shows empty state
5. **Case insensitive**: Visit `/?with=yash` - matches "Yash"
6. **Dropdown selection**: Select companion from dropdown, URL updates
7. **Clear filter**: Select "(none)", returns to full view

### Verify Stats

For a known companion filter:
1. Manually count films with that companion in log.csv
2. Verify poster count matches
3. Verify top directors/languages match filtered set

---

## Migration

No migration needed - this is additive. Existing URLs (`/`) continue to work unchanged.

---

## Example

### Current log.csv entries with Yash:

```csv
3/10/2024,Snowpiercer,600 Park Pl,Yash,110415
```

### Visiting `/?with=Yash`:

**Header:** `s4s with Yash`

**Posters:** Only Snowpiercer

**Stats:**
```
1 film
last watched 3/10/2024

top directors          top languages       top decades
Bong Joon-ho: 1        ko: 1               2010s: 1
```

---

## Summary

Phase 1 adds client-side companion filtering to the static site:
- URL-driven filtering (`?with=Name`)
- Embedded JSON data for client-side rendering
- Companion dropdown selector
- Dynamic stats for filtered view
- No backend required, preserves static architecture
