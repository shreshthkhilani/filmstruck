# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build the CLI
dotnet build src/FilmStruck.Cli/FilmStruck.Cli.csproj

# Run commands without installing
dotnet run --project src/FilmStruck.Cli/FilmStruck.Cli.csproj -- <command>
# Examples:
dotnet run --project src/FilmStruck.Cli/FilmStruck.Cli.csproj -- build
dotnet run --project src/FilmStruck.Cli/FilmStruck.Cli.csproj -- add
dotnet run --project src/FilmStruck.Cli/FilmStruck.Cli.csproj -- --help

# Pack and install as global tool for testing
cd src/FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli

# Update after changes (must uninstall first)
dotnet tool uninstall --global FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

**Environment:** Requires `TMDB_API_KEY` environment variable for TMDB commands.

## Development Commands

Use `make` for all development tasks:

```bash
make build      # Build the CLI
make test       # Run integration tests
make clean      # Clean build artifacts
make reinstall  # Reinstall CLI globally
make help       # Show available commands
```

The test suite dynamically generates a test site, runs CLI commands against it, and cleans up afterward. Tests run with or without `TMDB_API_KEY` (TMDB-dependent tests are skipped if not set).

## Architecture

FilmStruck is a .NET 9 CLI tool (Spectre.Console.Cli) that helps users track films with TMDB integration and generates a static site for GitHub Pages.

### Data Flow

```
User → CLI commands → TMDB API → CSV files (data/) → CLI build → index.html
```

### Key Patterns

**Commands** (`Commands/`): Orchestrate user interactions using Spectre.Console prompts. Each command extends `AsyncCommand<Settings>` and is registered in `Program.cs`.

**Services** (`Services/`): Contain business logic:
- `CsvService` - CSV read/write, auto-finds repo root via `.git` directory
- `TmdbService` - TMDB API client with bearer token auth
- `SiteGeneratorService` - HTML generation from embedded templates
- `StatsService` - Statistics calculation
- `PosterSelectionService` - Interactive poster selection UI

**Models** (`Models.cs`): Data classes including `Film`, `ApprovedFilm`, and TMDB DTOs with `JsonPropertyName` attributes.

**Templates** (`Templates/`): Embedded resources (HTML/CSS/JS) with placeholders like `{{USERNAME}}`, `{{FILMS_DATA}}`, `{{STYLES}}`, `{{APP_JS}}`.

### Adding a New Command

1. Create `src/FilmStruck.Cli/Commands/YourCommand.cs` implementing `AsyncCommand<YourCommand.Settings>`
2. Register in `Program.cs`: `config.AddCommand<YourCommand>("yourcommand").WithDescription("...");`
3. **Add integration tests** in `scripts/test.sh` to verify the command works end-to-end

### Data Files

- `data/log.csv` - Watch log: date (M/d/yyyy), title, location, companions, tmdbId
- `data/films.csv` - TMDB metadata cache: tmdbId, title, director, releaseYear, language, posterPath
- `data/stats.csv` - Aggregated statistics
- `filmstruck.json` - Configuration: username, siteTitle

## Git Conventions

Use conventional branch naming: prefix branches with `feature/`, `bugfix/`, `hotfix/`, `release/`, `chore/`, etc.

Examples: `feature/import-letterboxd`, `bugfix/csv-parsing`, `chore/update-deps`

## NuGet Publishing

Version is derived from git tags, not from .csproj. Publishing is fully automated:

- **Merge to main**: Auto-calculates version from PR labels, publishes to NuGet, creates GitHub Release
- **Push to `release/*`**: Publishes pre-release versions (`{latest-tag}-rc.{run_number}`)
- **Manual dispatch**: Allows publishing a specific version via workflow_dispatch

### PR Labels for Version Control

Add one of these labels to your PR to control the version bump:

| Label | Effect | Example |
|-------|--------|---------|
| `patch` (default) | Bug fixes, small changes | 1.3.2 → 1.3.3 |
| `minor` | New features | 1.3.2 → 1.4.0 |
| `major` | Breaking changes | 1.3.2 → 2.0.0 |

If no label is specified, defaults to `patch`.

### Developer Workflow

1. Create feature branch, make changes
2. Open PR to `main`
3. Add label: `patch`, `minor`, or `major` (optional, defaults to `patch`)
4. CI runs tests
5. Merge → auto-calculates version, publishes to NuGet, creates GitHub Release

No manual version editing required.
