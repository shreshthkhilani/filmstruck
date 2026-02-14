# FilmStruck

[![NuGet](https://img.shields.io/nuget/v/FilmStruck.Cli)](https://www.nuget.org/packages/FilmStruck.Cli)

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
3. Choose poster (displays clickable URLs - Cmd+click to preview)
4. Date watched
5. Location
6. Companions (optional)

**Options:**

| Flag | Short | Description |
|------|-------|-------------|
| `--title` | `-t` | Film title for TMDB search |
| `--date` | `-d` | Watch date in M/d/yyyy format (default: today) |
| `--location` | `-l` | Where the film was watched |
| `--companions` | `-c` | Comma-separated list of companions |
| `--default-poster` | | Use the default/first poster without prompting |
| `--tmdb-id` | | TMDB movie ID (skips search and confirmation) |

Any flags provided will skip the corresponding interactive prompt. This allows for scripted or partially automated usage:

```bash
# Fully automated (no prompts)
filmstruck add --tmdb-id 27053 -d 1/29/2026 -l Home -c "Alice,Bob" --default-poster

# Search by title, skip other prompts
filmstruck add -t "The Matrix" -d 1/15/2026 -l "AMC Theater" -c ""

# Just use default poster, prompt for everything else
filmstruck add --default-poster
```

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

### `filmstruck import-letterboxd-diary`

Import films from a Letterboxd diary CSV export.

```bash
filmstruck import-letterboxd-diary diary.csv
```

**How to export your Letterboxd diary:**

1. Go to [letterboxd.com](https://letterboxd.com) and log in
2. Navigate to **Settings** → **Import & Export**
3. Click **Export Your Data**
4. Download and extract the ZIP file
5. Use the `diary.csv` file from the export

The command will:
- Parse each diary entry and convert dates to FilmStruck format
- Skip entries that already exist in your log (by title + date)
- Prompt you to confirm each film before adding
- Search TMDB and let you select the correct match
- Allow poster selection when multiple options are available
- Prompt for location (with recent location suggestions) and companions
- Save to `log.csv` and `films.csv`, then recalculate stats

### `filmstruck hearts`

Manage your favorite films. Favorites are stored in `data/hearts.csv` and displayed in a dedicated section on your site.

#### `filmstruck hearts add`

Add a film to your favorites.

```bash
# Interactive: select from your logged films
filmstruck hearts add

# Direct: add by TMDB ID
filmstruck hearts add --tmdb-id 550
```

#### `filmstruck hearts remove`

Remove a film from your favorites.

```bash
# Interactive: select from your favorites
filmstruck hearts remove

# Direct: remove by TMDB ID
filmstruck hearts remove --tmdb-id 550
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

## Sample Site

See a fully populated example at [s4s-filmstruck](https://github.com/shreshthkhilani/s4s-filmstruck):

- **Live site:** https://shreshthkhilani.github.io/s4s-filmstruck
- 200+ films with metadata
- Companion filtering in action
- Custom favicon example

## API

FilmStruck also provides a public API for accessing watch log data, backed by AWS Lambda and DynamoDB.

### `GET /api/{username}`

Returns a user's enriched watch log (log entries joined with TMDB film metadata), sorted newest-first.

**Production:** `https://filmstruck.net/api/{username}`
**Staging:** `https://staging.filmstruck.net/api/{username}`

Example response:

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

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on developing the CLI and API.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for technical specification.

See [CHANGELOG.md](CHANGELOG.md) for version history.

## License

MIT License - see [LICENSE](LICENSE) for details.
