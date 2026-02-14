# FilmStruck - Architecture & Technical Specification

## Overview

FilmStruck is a .NET tool for tracking your film watching history with TMDB integration. It includes a CLI that generates a static HTML site for GitHub Pages, and a public API backed by AWS Lambda and DynamoDB.

**Repository:** https://github.com/shreshthkhilani/filmstruck (CLI source code)

**Sample Site:** https://github.com/shreshthkhilani/s4s-filmstruck (example user repository)
- Live at: https://shreshthkhilani.github.io/s4s-filmstruck

---

## User Repository Structure

When users run `filmstruck init`, they get this structure:

```
my-film-log/
├── data/
│   ├── log.csv              # Watch log (user entries)
│   ├── films.csv            # TMDB metadata cache
│   └── stats.csv            # Aggregated statistics (optional)
├── .github/
│   └── workflows/
│       └── deploy.yml       # GitHub Pages deployment
├── filmstruck.json          # Configuration
├── favicon.png              # Optional custom favicon
└── index.html               # Generated output (gitignored)
```

### Data Flow

```
User → CLI (add/enrich) → TMDB API → CSV files → CLI (build) → index.html
```

---

## CLI Architecture

### Technology Stack

- **.NET 9** - Runtime
- **Spectre.Console** - Terminal UI (prompts, colors, spinners)
- **Spectre.Console.Cli** - Command parsing
- **System.Net.Http.Json** - TMDB API calls

### Project Structure

```
src/FilmStruck.Cli/
├── Program.cs                    # Entry point, command registration
├── Models.cs                     # Data models and TMDB DTOs
├── Commands/
│   ├── InitCommand.cs           # filmstruck init
│   ├── AddCommand.cs            # filmstruck add
│   ├── EnrichCommand.cs         # filmstruck enrich
│   ├── BuildCommand.cs          # filmstruck build
│   └── CalculateCommand.cs      # filmstruck calculate
├── Services/
│   ├── ConfigService.cs         # Load/save filmstruck.json
│   ├── CsvService.cs            # CSV read/write operations
│   ├── TmdbService.cs           # TMDB API client
│   ├── SiteGeneratorService.cs  # HTML generation from templates
│   ├── StatsService.cs          # Statistics calculation
│   └── PosterSelectionService.cs # Interactive poster selection
├── Templates/                    # Embedded HTML/CSS/JS templates
│   ├── index.html
│   ├── styles.css
│   └── app.js
└── Resources/                    # Embedded init templates
    ├── deploy.yml
    └── gitignore
```

### Commands

#### `filmstruck init`

Initializes a new filmstruck repository.

Options:
- `-u, --username <name>` - Username displayed on the site
- `-f, --force` - Overwrite existing files

Creates: `filmstruck.json`, `data/log.csv`, `data/films.csv`, `.github/workflows/deploy.yml`, `.gitignore`

#### `filmstruck add`

Adds a new film entry with full TMDB lookup.

Flow:
1. Prompt for title
2. Search TMDB, display results with director info
3. User selects film or enters TMDB ID manually
4. Fetch poster options from TMDB images API
5. User selects poster (with browser preview option)
6. Prompt for date (default: today)
7. Prompt for location (recent locations as suggestions)
8. Prompt for companions
9. Append to log.csv, update films.csv

#### `filmstruck enrich`

Enriches existing log entries that lack TMDB IDs.

Flow:
1. Load log.csv and films.csv
2. Sync any entries with tmdbId but missing from films.csv
3. For each entry without tmdbId:
   - Search TMDB
   - User selects film or skips
   - User selects poster
   - Save immediately (allows resuming)

#### `filmstruck build`

Generates the static site from CSV data.

Flow:
1. Load config from `filmstruck.json`
2. Load log.csv and films.csv
3. Join entries with metadata
4. Generate HTML with embedded data
5. Write index.html

#### `filmstruck calculate`

Recalculates statistics from log and films data.

Flow:
1. Load log.csv and films.csv
2. Aggregate counts for each stat category
3. Write stats.csv
4. Display summary

### Services

#### ConfigService

Loads and saves `filmstruck.json` configuration.

```csharp
public class FilmStruckConfig
{
    public string Username { get; set; } = "user";
    public string SiteTitle { get; set; } = "filmstruck";
}
```

#### CsvService

Handles all CSV file operations.

- `LoadLogAsync()` - Parse log.csv into List<Film>
- `LoadApprovedFilmsAsync()` - Parse films.csv into Dictionary<int, ApprovedFilm>
- `WriteLogAsync(films)` - Write log.csv
- `WriteApprovedFilmsAsync(approved)` - Write films.csv
- `GetRecentLocations(films, count)` - Get most common locations for suggestions

Automatically finds repo root by searching for `.git` directory.

#### TmdbService

TMDB API client using bearer token authentication.

- `SearchMoviesAsync(title)` - Search movies by title
- `GetMovieDetailsAsync(movieId)` - Get movie details
- `GetDirectorAsync(movieId)` - Get director(s) from credits
- `GetMoviePostersAsync(movieId)` - Get available posters sorted by rating
- `GetApprovedFilmAsync(tmdbId, fallbackTitle)` - Get full metadata

#### SiteGeneratorService

Generates HTML from embedded templates, replacing placeholders:

- `{{USERNAME}}` - User's configured username
- `{{STYLES}}` - CSS content
- `{{FILMS_DATA}}` - JSON array of films
- `{{COMPANIONS_DATA}}` - JSON array of companions
- `{{APP_JS}}` - JavaScript content

#### PosterSelectionService

Interactive poster selection UI.

Features:
- Shows poster metadata (resolution, language, rating)
- "Preview" option opens TMDB poster gallery in browser
- "Default" option uses highest-rated poster
- "Skip" option keeps current poster

---

## Data Schema

### log.csv (Watch Log)

Primary user data - each row is a film viewing event.

| Column | Type | Description |
|--------|------|-------------|
| date | string | Watch date in M/d/yyyy format |
| title | string | Film title as entered by user |
| location | string | Where the film was watched |
| companions | string | Comma-separated list of people watched with |
| tmdbId | int? | TMDB movie ID (nullable for unenriched entries) |

### films.csv (TMDB Metadata)

Cached metadata from TMDB, keyed by tmdbId.

| Column | Type | Description |
|--------|------|-------------|
| tmdbId | int | TMDB movie ID (primary key) |
| title | string | Official title from TMDB |
| director | string | Comma-separated director names |
| releaseYear | string | 4-digit year |
| language | string | ISO 639-1 language code |
| posterPath | string | TMDB poster path (e.g., /abc123.jpg) |

### filmstruck.json (Configuration)

```json
{
  "username": "alice",
  "siteTitle": "filmstruck"
}
```

---

## Static Site Features

### Poster Grid

- Films sorted by date (reverse chronological)
- TMDB poster images from CDN (`https://image.tmdb.org/t/p/w154/`)
- Hover tooltips with film details
- Scale effect on hover

### Companion Filtering

Client-side filtering via URL parameters:

| URL | Behavior |
|-----|----------|
| `/` | All films, all stats |
| `/?with=Alice` | Films watched with Alice |
| `/?with=Alice,Bob` | Films watched with both Alice AND Bob |

Features:
- Dropdown companion selector
- Dynamic header: "username" → "username ♥ alice"
- Stats recalculate for filtered view
- Case-insensitive matching
- URL updates on filter change (supports browser back/forward)

### Statistics Footer

Displays:
- Total films count
- Films watched this year
- Last watched date
- Top directors
- Top languages
- Top decades
- Top companions (or "also with" when filtering)

---

## TMDB API Integration

### Authentication

Bearer token via `TMDB_API_KEY` environment variable.

### Endpoints Used

| Endpoint | Purpose |
|----------|---------|
| `GET /3/search/movie` | Search films by title |
| `GET /3/movie/{id}` | Get movie details |
| `GET /3/movie/{id}/credits` | Get cast/crew for director |
| `GET /3/movie/{id}/images` | Get available posters |

### Rate Limiting

50ms delay between director lookups to avoid hitting rate limits.

---

## Deployment

### GitHub Pages (via GitHub Actions)

The `filmstruck init` command creates a workflow that:
1. Installs FilmStruck CLI from NuGet
2. Runs `filmstruck build`
3. Deploys to GitHub Pages

Users enable Pages with "GitHub Actions" as the source.

### NuGet Distribution

The CLI is packaged as a .NET global tool:

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>filmstruck</ToolCommandName>
```

Install: `dotnet tool install --global FilmStruck.Cli`

---

## User Setup Guide

### Prerequisites

- [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- A GitHub account
- A [TMDB API key](https://www.themoviedb.org/settings/api) (free)

### Quick Start

```bash
# Install CLI
dotnet tool install --global FilmStruck.Cli

# Create repository
mkdir my-films && cd my-films
filmstruck init --username yourname

# Set TMDB API key
export TMDB_API_KEY="your-key"

# Add films
filmstruck add

# Build and preview
filmstruck build
open index.html

# Deploy
git init && git add . && git commit -m "Initial commit"
gh repo create my-films --public --push --source .
# Enable GitHub Pages with "GitHub Actions" source
```

### Adding Films

```bash
filmstruck add
git add data/
git commit -m "Add: Film Name"
git push
```

### Bulk Import

Edit `data/log.csv` directly, then run `filmstruck enrich` to add TMDB IDs.

### Troubleshooting

**"TMDB_API_KEY not set"** - Export the environment variable and reload shell.

**"Could not find repository root"** - Run from within a git repository.

**Film not found on TMDB** - Try original title or include year: "Amélie 2001"

---

## Sample Repository

The [s4s-filmstruck](https://github.com/shreshthkhilani/s4s-filmstruck) repository demonstrates a fully populated film log:

- **Live site:** https://shreshthkhilani.github.io/s4s-filmstruck
- Contains 200+ films with metadata
- Shows companion filtering in action
- Example of custom favicon

Use it as a reference for:
- Expected CSV format
- Configuration setup
- GitHub Actions workflow

---

## API Architecture

### Overview

The FilmStruck API is a .NET 9 minimal API that runs as an AWS Lambda behind API Gateway. It provides a `GET /api/{username}` endpoint returning enriched watch log data from DynamoDB.

### Request Flow

```
Route 53 (filmstruck.net / staging.filmstruck.net)
  → API Gateway (REST API, custom domain, ACM cert)
    → /api/{username} → Lambda (C# minimal API)
      → DynamoDB (filmstruck-{env})
```

### Project Structure

```
src/FilmStruck.Api/
├── Program.cs                    # Minimal API entry point
├── Models/
│   ├── LogItem.cs               # DynamoDB log record model
│   ├── FilmItem.cs              # DynamoDB film metadata model
│   └── LogResponse.cs           # API response DTOs
└── Services/
    └── WatchLogService.cs       # DDB query + Log/Film join logic
```

### DynamoDB Table Design

Single table design using `filmstruck-{env}` with composite keys:

| Key | Type | Description |
|-----|------|-------------|
| PartitionKey | String | Username (e.g., `shreshth`) |
| SortKey | String | Model-type-prefixed composite key |

**Model types stored in the table:**

| Model | SortKey Format | Attributes |
|-------|---------------|------------|
| Log | `Log#{yyyy-MM-dd}#{tmdbId}` | date, title, location, companions, tmdbId |
| Film | `Film#{tmdbId}` | tmdbId, title, director, releaseYear, language, posterPath |

Billing mode: PAY_PER_REQUEST (on-demand).

### Query Strategy

The `WatchLogService` performs a single DynamoDB Query for all items where `PartitionKey = {username}`. It then:

1. Separates results by SortKey prefix into Log and Film items
2. Builds a lookup dictionary of Film items keyed by tmdbId
3. Joins each Log entry with its corresponding Film metadata
4. Returns entries sorted by date descending

### API Response

```json
{
  "username": "alice",
  "count": 2,
  "entries": [
    {
      "date": "2/10/2025",
      "title": "Pulp Fiction",
      "location": "Home",
      "companions": "Bob",
      "tmdbId": "680",
      "director": "Quentin Tarantino",
      "releaseYear": "1994",
      "language": "en",
      "posterPath": "/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg"
    }
  ]
}
```

### Local Development

The API supports local development via DynamoDB Local (Docker):

- Set `AWS_ENDPOINT_URL=http://localhost:8000` to use DynamoDB Local
- Set `TABLE_NAME` to specify which table to query
- `make api-run` sets both automatically
- `./scripts/local-setup.sh` creates the table and seeds sample data

---

## AWS Infrastructure (CDK)

Infrastructure is defined in `infra/` using AWS CDK (TypeScript).

### Stack Resources

| Resource | Description |
|----------|-------------|
| DynamoDB Table | Single table with PartitionKey + SortKey, on-demand billing |
| Lambda Function | .NET 9 runtime, reads TABLE_NAME env var |
| API Gateway | REST API with `/api/{username}` route, CORS enabled |
| ACM Certificate | `filmstruck.net` + `*.filmstruck.net`, DNS validation |
| Route 53 Records | A/AAAA alias records pointing to API Gateway custom domain |

### Environments

| Property | Staging | Production |
|----------|---------|------------|
| Domain | `staging.filmstruck.net` | `filmstruck.net` |
| Table | `filmstruck-staging` | `filmstruck-prod` |
| Lambda memory | 128 MB | 256 MB |
| Removal policy | DESTROY | RETAIN |

### Deployment

```bash
# First-time setup
aws configure
cd infra && npx cdk bootstrap

# Deploy
make infra-deploy-staging
make infra-deploy-prod
```
