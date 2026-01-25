# FilmStruck Technical Specification

## Overview

FilmStruck is a personal film tracking application that combines a .NET CLI for data entry with a static HTML site for display. Films are stored in CSV files enriched with metadata from The Movie Database (TMDB) API.

## Architecture

```
filmstruck/
├── data/
│   ├── log.csv          # Watch log (user entries)
│   ├── films.csv        # TMDB metadata cache
│   └── stats.csv        # Aggregated statistics
├── src/
│   └── FilmStruck.Cli/  # .NET CLI application
├── build.js             # Static site generator
└── index.html           # Generated output (GitHub Pages)
```

### Data Flow

```
User → CLI (add/enrich) → TMDB API → CSV files → build.js → index.html
```

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

### stats.csv (Aggregated Statistics)

Pre-computed statistics for the static site.

| Column | Type | Description |
|--------|------|-------------|
| stat | string | Statistic category |
| key | string | Dimension value |
| value | int | Count |

Stat categories:
- `watch_year` - Films per year watched (includes ALL_TIME total)
- `director` - Films per director
- `language` - Films per original language
- `companion` - Films per companion
- `location` - Films per location
- `release_decade` - Films per release decade (e.g., "1990s")

## CLI Application

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
│   ├── AddCommand.cs            # filmstruck add
│   ├── EnrichCommand.cs         # filmstruck enrich
│   └── CalculateCommand.cs      # filmstruck calculate
└── Services/
    ├── CsvService.cs            # CSV read/write operations
    ├── TmdbService.cs           # TMDB API client
    ├── StatsService.cs          # Statistics calculation
    └── PosterSelectionService.cs # Interactive poster selection
```

### Commands

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
10. Recalculate and write stats.csv

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

#### `filmstruck calculate`

Recalculates statistics from log and films data.

Flow:
1. Load log.csv and films.csv
2. Aggregate counts for each stat category
3. Write stats.csv
4. Display summary

### Services

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
- `GetMovieOptionsAsync(searchResults)` - Enrich search results with director info
- `GetApprovedFilmAsync(tmdbId, fallbackTitle)` - Get full metadata for approval

#### StatsService

Calculates aggregated statistics.

- `CalculateStats(log, films)` - Compute all stat categories
- `WriteStatsAsync(path, stats)` - Write stats.csv

#### PosterSelectionService

Interactive poster selection UI.

- `SelectPoster(title, year, movieId, posters, currentPoster)` - Display selection prompt

Features:
- Shows poster metadata (resolution, language, rating)
- "Preview" option opens TMDB poster gallery in browser
- "Default" option uses highest-rated poster
- "Skip" option keeps current poster
- Loops until selection made (for browser preview workflow)

### Models

```csharp
// User entry in log.csv
class Film(date, title, location, companions, tmdbId?)

// TMDB metadata in films.csv
record ApprovedFilm(TmdbId, Title, Director?, ReleaseYear?, Language?, PosterPath?)

// For displaying search results with director info
record MovieOption(Movie, Director?, Year)

// TMDB API responses
record TmdbSearchResponse(Results: List<TmdbMovie>?)
record TmdbMovie(Id, Title?, ReleaseDate?, OriginalLanguage?, PosterPath?)
record TmdbCreditsResponse(Crew: List<TmdbCrewMember>?)
record TmdbCrewMember(Name?, Job?)
record TmdbImagesResponse(Posters: List<TmdbPoster>?)
record TmdbPoster(FilePath?, Width, Height, Language?, VoteAverage, VoteCount)
```

## Static Site Generator (build.js)

Node.js script that generates index.html from CSV data.

### Process

1. Parse log.csv, films.csv, stats.csv
2. Join log entries with film metadata
3. Sort by date (reverse chronological)
4. Generate HTML with:
   - Poster grid (TMDB image URLs)
   - Hover effect (scale on hover)
   - Footer with stats summary

### Output

Single-page HTML with:
- Inline CSS (no external dependencies)
- Poster images from TMDB CDN (`https://image.tmdb.org/t/p/w154/`)
- Statistics footer (total films, top companions/directors/languages)

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

## Deployment

### GitHub Pages

The site is deployed via GitHub Pages from the `main` branch.

Workflow:
1. Run CLI commands to update CSVs
2. Run `node build.js` to regenerate index.html
3. Commit and push to main
4. GitHub Pages serves index.html

### CLI Distribution

Packaged as a .NET global tool:

```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>filmstruck</ToolCommandName>
```

Install: `dotnet tool install --global --add-source ./nupkg FilmStruck.Cli`

## Future Considerations

- GitHub Action to auto-rebuild site on CSV changes
- Search/filter on the static site
- Year-in-review statistics page
- Export to Letterboxd format
