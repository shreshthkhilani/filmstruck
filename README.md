# FilmStruck

Films watched by Sophie and Shreshth.

https://shreshthkhilani.github.io/filmstruck

## Data

- `data/log.csv` - watch log (date, title, location, companions, tmdbId)
- `data/films.csv` - TMDB metadata (tmdbId, title, director, releaseYear, language, posterPath)

## Build

```bash
node build.js
```

Generates `index.html` from the CSVs.

## CLI

The FilmStruck CLI provides two commands for managing your watch log:

### Setup

Get a TMDB API key at https://www.themoviedb.org/settings/api

```bash
export TMDB_API_KEY="your-api-key"
```

### Add a new film

```bash
cd src/FilmStruck.Cli
dotnet run -- add
```

Interactive prompts will ask for:
1. Film title (searches TMDB)
2. Select from search results or enter TMDB ID manually
3. Date watched (default: today)
4. Location (with recent locations as suggestions)
5. Companions (optional)

### Enrich existing entries

Look up TMDB IDs for films in log.csv that don't have one:

```bash
cd src/FilmStruck.Cli
dotnet run -- enrich
```

### Install as global tool

Package and install the CLI as a dotnet global tool:

```bash
cd src/FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

Then run from anywhere:

```bash
filmstruck add
filmstruck enrich
```

To uninstall:

```bash
dotnet tool uninstall --global FilmStruck.Cli
```
