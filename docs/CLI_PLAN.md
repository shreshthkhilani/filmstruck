# FilmStruck CLI Plan

## Goal

Convert the existing `FilmStruck.Enricher` into a proper CLI with two commands:
1. `filmstruck add` - Add a new film entry with TMDB lookup
2. `filmstruck enrich` - Run enricher on existing log.csv entries without tmdbId

## Current State

- `data/log.csv` - watch log (date, title, location, companions, tmdbId)
- `data/films.csv` - TMDB metadata (tmdbId, title, director, releaseYear, language, posterPath)
- `src/FilmStruck.Cli/` - CLI with TMDB search/select logic
- `build.js` - generates static HTML from CSVs

## Implementation

### 1. Add Spectre.Console for CLI

Add `Spectre.Console` NuGet package for:
- Command parsing (add/enrich subcommands)
- Interactive prompts (text input, selection menus)
- Better terminal UX (colors, spinners)

### 2. Refactor to Command Structure

Reorganize `Program.cs`:

```
filmstruck/
└── src/
    └── FilmStruck.Cli/
        ├── Program.cs           # Entry point with command routing
        ├── Commands/
        │   ├── AddCommand.cs    # filmstruck add
        │   └── EnrichCommand.cs # filmstruck enrich
        └── Services/
            ├── TmdbService.cs   # TMDB API calls
            └── CsvService.cs    # CSV read/write
```

### 3. `filmstruck add` Command

Flow:
1. Prompt for title
2. Search TMDB, display results with Spectre.Console selection
   - Include "Enter TMDB ID manually" option for when search doesn't find the right film
   - If manual ID selected, prompt for ID and fetch/confirm details
3. Prompt for date (default: today)
4. Prompt for location (with recent values as suggestions)
5. Prompt for companions (optional)
6. Append to log.csv with tmdbId
7. Add to films.csv if new tmdbId

### 4. `filmstruck enrich` Command

Same as current enricher:
1. Read log.csv
2. Find entries without tmdbId
3. For each: search TMDB, prompt selection, update CSVs

### 5. Install as Global Tool

Configure `.csproj` for `dotnet tool install`:
```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>filmstruck</ToolCommandName>
```

Install locally: `dotnet tool install --global --add-source ./nupkg FilmStruck.Cli`

## Files Modified

| File | Changes |
|------|---------|
| `src/FilmStruck.Enricher/` | Renamed to `FilmStruck.Cli/` |
| `FilmStruck.Cli.csproj` | Added Spectre.Console, configured as dotnet tool |
| `Program.cs` | Refactored to use Spectre.Console commands |
| `README.md` | Updated CLI instructions |
| `docs/CLI_PLAN.md` | This plan document |

## Verification

1. `cd src/FilmStruck.Cli && dotnet run -- add` - interactive add flow
2. `dotnet run -- enrich` - enricher flow
3. After adding, run `node build.js` and verify poster appears
4. Package and install: `dotnet pack && dotnet tool install ...`
5. Run `filmstruck add` from anywhere
