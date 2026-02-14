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

## API (FilmStruck.Api)

A .NET 9 minimal API that runs as an AWS Lambda behind API Gateway. Provides a `GET /api/{username}` endpoint that returns enriched watch log data from DynamoDB.

### API Development

```bash
make api-build    # Build the API project
make api-run      # Run API locally (requires DynamoDB Local)
make local-up     # Start DynamoDB Local + seed test data
```

### Local Development

1. Start DynamoDB Local: `docker compose up -d`
2. Seed test data: `./scripts/local-setup.sh`
3. Run API: `make api-run`
4. Test: `curl http://localhost:5000/api/testuser`

### DynamoDB Table Design

Single table (`filmstruck-{env}`) with `PartitionKey` (username) and `SortKey` (model-type-prefixed):
- Log entries: `Log#{yyyy-MM-dd}#{tmdbId}`
- Film metadata: `Film#{tmdbId}`

### AWS Infrastructure (CDK)

Infrastructure is defined in `infra/` using AWS CDK (TypeScript).

```bash
make infra-install        # Install CDK dependencies
make infra-synth          # Synthesize CloudFormation template
make infra-deploy-staging # Deploy to staging
make infra-deploy-prod    # Deploy to production
```

**Prerequisites:** `aws configure` with appropriate credentials, `cdk bootstrap` for first-time setup.

**Environments:**
- **Staging**: `staging.filmstruck.net`, 128MB Lambda, DESTROY removal policy
- **Prod**: `filmstruck.net`, 256MB Lambda, RETAIN removal policy

## Git Conventions

Use conventional branch naming: prefix branches with `feature/`, `bugfix/`, `hotfix/`, `release/`, `chore/`, etc.

Examples: `feature/import-letterboxd`, `bugfix/csv-parsing`, `chore/update-deps`

## NuGet Publishing

Version is derived from git tags, not from .csproj. Publishing is fully automated:

- **Merge to main**: Auto-calculates version from PR labels, publishes to NuGet, creates GitHub Release
- **Manual dispatch**: Select bump type (patch/minor/major) to publish the next version

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
2. Open PR to `main` with a version label: `patch`, `minor`, or `major`
3. CI runs tests on PR
4. Merge → CI runs again via publish workflow → auto-calculates version, publishes to NuGet, creates GitHub Release

**Important:** Always add a version label (`patch`, `minor`, or `major`) when creating a PR.

### Manual Release

Use the workflow_dispatch trigger to manually publish:
1. Go to Actions → "Publish to NuGet"
2. Click "Run workflow"
3. Select bump type (patch/minor/major)
4. The workflow calculates the next version, runs tests, publishes, and creates a GitHub Release

No manual version editing required.
