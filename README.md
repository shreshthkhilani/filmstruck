# FilmStruck

Films watched by Sophie and Shreshth.

https://shreshthkhilani.github.io/filmstruck

## Data

- `data/log.csv` - watch log (date, title, location, companions, tmdbId)
- `data/films.csv` - TMDB metadata (tmdbId, title, director, releaseYear, language, posterPath)
- `data/stats.csv` - aggregated statistics (watch_year, director, language, companion, location, release_decade)

## CLI

The FilmStruck CLI provides commands for managing your watch log with TMDB integration.

### Setup

1. Get a TMDB API key at https://www.themoviedb.org/settings/api

2. Set the environment variable:
```bash
export TMDB_API_KEY="your-api-key"
```

### Install as global tool

```bash
cd src/FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

To update after code changes:
```bash
cd src/FilmStruck.Cli
dotnet tool uninstall --global FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

### Commands

#### `filmstruck add`

Add a new film entry with TMDB lookup.

```bash
filmstruck add
```

Interactive prompts:
1. Film title (searches TMDB)
2. Select from search results or enter TMDB ID manually
3. **Poster selection** - choose from available posters with metadata (resolution, language, rating) or open TMDB in browser to preview
4. Date watched (default: today)
5. Location (with recent locations as suggestions)
6. Companions (optional)

#### `filmstruck enrich`

Look up TMDB IDs for films in log.csv that don't have one:

```bash
filmstruck enrich
```

For each film without a TMDB ID:
- Search TMDB and display results
- Select the correct film or enter ID manually
- Choose poster from available options
- Saves progress after each film

#### `filmstruck calculate`

Calculate and write statistics to stats.csv:

```bash
filmstruck calculate
```

Generates aggregated stats for: watch year, directors, languages, companions, locations, and release decades.

#### `filmstruck build`

Generate the static site from CSV data:

```bash
filmstruck build
```

Generates `index.html` in the repo root by:
- Loading watch log and film metadata from CSVs
- Joining data with TMDB poster paths
- Rendering HTML from templates in `src/FilmStruck.Cli/Templates/`

### Poster Selection

When adding or enriching films, you can select from multiple poster options:

```
Select poster for "Amelie" (2001):

> [Default] Use TMDB's primary poster
  [Preview] Open posters in browser
  ────────────────────────────────
  2000x3000 - English - ★ 5.3 (12 votes)
  1400x2100 - No text - ★ 5.1 (8 votes)
  1000x1500 - French - ★ 4.9 (6 votes)
  ────────────────────────────────
  [Skip] Keep current poster
```

- **[Default]** - Use TMDB's highest-rated poster
- **[Preview]** - Opens the TMDB poster gallery in your browser to view images, then return to make selection
- **Individual posters** - Shows resolution, language, and community rating
- **[Skip]** - Keep the current poster (useful when enriching)

### Development

Run without installing:

```bash
cd src/FilmStruck.Cli
dotnet run -- add
dotnet run -- enrich
dotnet run -- calculate
dotnet run -- build
```

### Uninstall

```bash
dotnet tool uninstall --global FilmStruck.Cli
```
