# FilmStruck

A CLI tool for tracking your film watching history with TMDB integration. Generate a beautiful static site to showcase your film log.

## Quick Start

```bash
# Install the CLI
dotnet tool install --global FilmStruck.Cli

# Create a new film log repository
mkdir my-films && cd my-films
filmstruck init --username alice

# Set your TMDB API key
export TMDB_API_KEY="your-api-key"

# Add your first film
filmstruck add

# Build the static site
filmstruck build
```

## Installation

Requires [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) or later.

```bash
dotnet tool install --global FilmStruck.Cli
```

To update:
```bash
dotnet tool update --global FilmStruck.Cli
```

## Commands

### `filmstruck init`

Initialize a new filmstruck repository with the required structure.

```bash
filmstruck init --username yourname
```

Creates:
- `filmstruck.json` - Configuration file with your username
- `data/log.csv` - Your watch log
- `data/films.csv` - TMDB metadata cache
- `.github/workflows/deploy.yml` - GitHub Pages deployment workflow
- `.gitignore` - Ignores generated files

### `filmstruck add`

Add a new film entry with TMDB lookup.

```bash
filmstruck add
```

Interactive prompts guide you through:
1. Film title (searches TMDB)
2. Select from search results
3. Choose poster
4. Date watched
5. Location
6. Companions (optional)

### `filmstruck enrich`

Look up TMDB IDs for existing log entries that don't have one.

```bash
filmstruck enrich
```

### `filmstruck build`

Generate the static site from your CSV data.

```bash
filmstruck build
```

Outputs `index.html` in the repository root.

### `filmstruck calculate`

Calculate statistics and write to `data/stats.csv`.

```bash
filmstruck calculate
```

## Configuration

Configuration is stored in `filmstruck.json`:

```json
{
  "username": "alice",
  "siteTitle": "filmstruck"
}
```

## TMDB API Key

Get a free API key at [themoviedb.org/settings/api](https://www.themoviedb.org/settings/api).

Set it as an environment variable:
```bash
export TMDB_API_KEY="your-api-key"
```

Or add to your shell profile (`~/.bashrc`, `~/.zshrc`, etc.).

## Deployment

### GitHub Pages (Recommended)

The `filmstruck init` command creates a GitHub Actions workflow that automatically deploys your site when you push to `main`.

1. Push your repository to GitHub
2. Go to Settings > Pages
3. Set Source to "GitHub Actions"
4. Push a commit to trigger deployment

Your site will be available at `https://username.github.io/repo-name`

### Manual Deployment

Run `filmstruck build` and host the generated `index.html` anywhere that serves static files.

## Repository Structure

A filmstruck repository contains:

```
my-films/
├── data/
│   ├── log.csv              # Your watch log
│   └── films.csv            # TMDB metadata cache
├── .github/
│   └── workflows/
│       └── deploy.yml       # GitHub Pages workflow
├── filmstruck.json          # Configuration
├── favicon.png              # Optional custom favicon
└── index.html               # Generated (gitignored)
```

## Data Format

### log.csv

| Column | Description |
|--------|-------------|
| date | Watch date (M/D/YYYY) |
| title | Film title |
| location | Where you watched |
| companions | Comma-separated list of who you watched with |
| tmdbId | TMDB ID for metadata lookup |

### films.csv

Cached TMDB metadata, automatically populated when adding films.

## Example

See this repository's `data/` folder for an example of a populated film log.

Live site: https://shreshthkhilani.github.io/filmstruck

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on developing the CLI.

## License

MIT License - see [LICENSE](LICENSE) for details.
