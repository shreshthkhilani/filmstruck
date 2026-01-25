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

## Enricher

To look up TMDB IDs for new films:

```bash
export TMDB_API_KEY="your-api-key"
cd src/FilmStruck.Enricher
dotnet run
```

Get a TMDB API key at https://www.themoviedb.org/settings/api
