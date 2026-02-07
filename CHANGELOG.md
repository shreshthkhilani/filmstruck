# Changelog

## [1.3.0] - 2026-02-07

### Added
- **Hearts (favorites)**: New `filmstruck hearts add` and `filmstruck hearts remove` commands to mark films as favorites. Hearted films appear in a dedicated section on your generated site.


All notable changes to FilmStruck CLI will be documented in this file.

## [1.2.0] - 2026-02-01

### Added
- **Link-based poster selection**: Poster selection now displays clickable URLs for each poster option. Cmd+click (or Ctrl+click) to preview posters directly in your browser, then enter a number to select. Shows top 5 posters with resolution and language info.

### Fixed
- **Placeholder poster images**: Films without poster artwork now display a placeholder image instead of a broken image icon.
- **Same-day log ordering**: When multiple films are logged for the same date, the original entry order is now preserved in the generated site.

## [1.1.0] - 2025-01-29

### Added
- CLI flags for `filmstruck add` command (`--title`, `--date`, `--location`, `--companions`, `--default-poster`, `--tmdb-id`)
- `import-letterboxd-diary` command for importing Letterboxd diary exports

## [1.0.0] - 2025-01-15

### Added
- Initial release
- `init` command to create a new filmstruck repository
- `add` command with TMDB integration and poster selection
- `enrich` command to look up TMDB IDs for existing entries
- `build` command to generate static site
- `calculate` command for statistics
- GitHub Pages deployment workflow
