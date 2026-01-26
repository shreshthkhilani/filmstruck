# Contributing to FilmStruck

Thank you for your interest in contributing to FilmStruck!

## Development Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A TMDB API key from [themoviedb.org](https://www.themoviedb.org/settings/api)

### Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/shreshthkhilani/filmstruck.git
   cd filmstruck
   ```

2. Set your TMDB API key:
   ```bash
   export TMDB_API_KEY="your-api-key"
   ```

3. Run commands directly (without installing):
   ```bash
   cd src/FilmStruck.Cli
   dotnet run -- build
   dotnet run -- add
   ```

### Installing Locally

To test as a global tool:

```bash
cd src/FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

To update after changes:
```bash
cd src/FilmStruck.Cli
dotnet tool uninstall --global FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

## Project Structure

```
filmstruck/
├── src/
│   └── FilmStruck.Cli/
│       ├── Commands/           # CLI command implementations
│       ├── Services/           # Business logic
│       ├── Templates/          # HTML/CSS/JS templates (embedded)
│       ├── Resources/          # Init templates (embedded)
│       └── Program.cs          # Entry point
├── data/                       # Example film data
├── .github/
│   └── workflows/
│       ├── deploy.yml          # Deploys example site
│       └── publish.yml         # Publishes to NuGet
├── filmstruck.json             # Example config
└── README.md
```

## Key Files

### Commands

- `InitCommand.cs` - Bootstraps new repositories
- `AddCommand.cs` - Interactive film entry with TMDB search
- `EnrichCommand.cs` - Adds TMDB IDs to existing entries
- `BuildCommand.cs` - Generates the static site
- `CalculateCommand.cs` - Computes statistics

### Services

- `ConfigService.cs` - Loads/saves `filmstruck.json`
- `CsvService.cs` - Reads/writes CSV data files
- `TmdbService.cs` - TMDB API integration
- `SiteGeneratorService.cs` - HTML generation from templates

### Templates

Templates are embedded resources that get compiled into the DLL:

- `index.html` - Page structure with placeholders
- `styles.css` - Site styling
- `app.js` - Client-side interactivity

Placeholders:
- `{{USERNAME}}` - User's configured username
- `{{STYLES}}` - CSS content
- `{{FILMS_DATA}}` - JSON array of films
- `{{COMPANIONS_DATA}}` - JSON array of companions
- `{{APP_JS}}` - JavaScript content

## Making Changes

### Adding a New Command

1. Create `src/FilmStruck.Cli/Commands/YourCommand.cs`
2. Implement `Command<YourCommand.Settings>` or `AsyncCommand<...>`
3. Register in `Program.cs`:
   ```csharp
   config.AddCommand<YourCommand>("yourcommand")
       .WithDescription("Description here");
   ```

### Modifying the Site Output

1. Edit files in `src/FilmStruck.Cli/Templates/`
2. Run `dotnet run -- build` to test
3. Open `index.html` in a browser

### Adding Configuration Options

1. Add property to `FilmStruckConfig` class in `ConfigService.cs`
2. Use the config in relevant commands/services

## Code Style

- Use C# 12 features where appropriate
- Follow existing patterns in the codebase
- Keep commands focused on orchestration, put logic in services

## Testing

Currently there are no automated tests. Manual testing:

```bash
# Test init
mkdir /tmp/test-fs && cd /tmp/test-fs
dotnet run --project /path/to/filmstruck/src/FilmStruck.Cli -- init -u testuser

# Test build
cd /path/to/filmstruck
dotnet run --project src/FilmStruck.Cli -- build
open index.html
```

## Submitting Changes

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally
5. Submit a pull request

## Releasing

Releases are published to NuGet automatically when a GitHub release is created:

1. Update version in `FilmStruck.Cli.csproj`
2. Create a GitHub release with tag matching the version (e.g., `v1.0.1`)
3. The `publish.yml` workflow will push to NuGet.org

## Questions?

Open an issue on GitHub if you have questions or need help.
