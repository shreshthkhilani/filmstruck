# FilmStruck Setup Guide

This guide walks you through setting up your own film tracking site with FilmStruck.

## Prerequisites

- [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (or SDK)
- A GitHub account (for deployment)
- A [TMDB API key](https://www.themoviedb.org/settings/api) (free)

## Step 1: Install the CLI

```bash
dotnet tool install --global FilmStruck.Cli
```

Verify installation:
```bash
filmstruck --help
```

## Step 2: Create Your Repository

```bash
# Create a new directory for your film log
mkdir my-film-log
cd my-film-log

# Initialize with your username
filmstruck init --username yourname
```

This creates the following structure:
```
my-film-log/
├── data/
│   ├── log.csv          # Your watch log (empty)
│   └── films.csv        # TMDB metadata (empty)
├── .github/
│   └── workflows/
│       └── deploy.yml   # GitHub Pages deployment
├── filmstruck.json      # Configuration
└── .gitignore
```

## Step 3: Set Up TMDB API Key

1. Create a free account at [themoviedb.org](https://www.themoviedb.org/)
2. Go to [Settings > API](https://www.themoviedb.org/settings/api)
3. Request an API key (select "Developer" option)
4. Copy your API key (v3 auth)

Set the environment variable:
```bash
# Add to ~/.bashrc, ~/.zshrc, or equivalent
export TMDB_API_KEY="your-api-key-here"
```

Reload your shell or run `source ~/.bashrc`.

## Step 4: Add Your First Film

```bash
filmstruck add
```

Follow the prompts:
1. **Title**: Enter the film name to search TMDB
2. **Select film**: Choose from search results
3. **Poster**: Select a poster or use the default
4. **Date**: When you watched it (default: today)
5. **Location**: Where you watched (home, cinema name, etc.)
6. **Companions**: Who you watched with (comma-separated, optional)

## Step 5: Build and Preview

```bash
filmstruck build
```

This generates `index.html`. Open it in your browser to preview:
```bash
open index.html  # macOS
xdg-open index.html  # Linux
```

## Step 6: Deploy to GitHub Pages

### Create GitHub Repository

```bash
# Initialize git
git init
git add .
git commit -m "Initial commit"

# Create repo on GitHub and push
gh repo create my-film-log --public --push --source .
# Or manually create on github.com and push
```

### Enable GitHub Pages

1. Go to your repository on GitHub
2. Navigate to **Settings** > **Pages**
3. Under "Build and deployment", set:
   - Source: **GitHub Actions**
4. The workflow will run automatically on your next push

### Trigger Deployment

```bash
git push
```

Your site will be live at: `https://yourusername.github.io/my-film-log`

## Adding More Films

Whenever you watch a film:

```bash
cd my-film-log
filmstruck add
git add data/
git commit -m "Add: Film Name"
git push
```

The site will automatically rebuild and deploy.

## Customization

### Change Username

Edit `filmstruck.json`:
```json
{
  "username": "newusername",
  "siteTitle": "filmstruck"
}
```

Then rebuild: `filmstruck build`

### Add a Custom Favicon

Place a `favicon.png` file in your repository root. The deploy workflow will include it automatically.

### Bulk Import

If you have existing watch data, you can edit `data/log.csv` directly:

```csv
date,title,location,companions,tmdbId
1/15/2024,The Matrix,home,"alice,bob",603
1/20/2024,Inception,IMAX Cinema,alice,27205
```

Then run `filmstruck enrich` to fill in any missing TMDB IDs, and `filmstruck build` to regenerate the site.

## Troubleshooting

### "TMDB_API_KEY not set"

Make sure your environment variable is exported:
```bash
echo $TMDB_API_KEY
```

If empty, set it again and reload your shell.

### "Could not find repository root"

FilmStruck looks for a `.git` directory to find the repo root. Make sure you've run `git init` or are in a git repository.

### GitHub Pages not updating

1. Check the **Actions** tab on GitHub for workflow errors
2. Ensure Pages source is set to "GitHub Actions"
3. Try a manual workflow dispatch from the Actions tab

### Film not found on TMDB

Try:
- Different spelling or original title
- Include the year: "Amélie 2001"
- Search TMDB website directly to find the correct title

## Uninstalling

```bash
dotnet tool uninstall --global FilmStruck.Cli
```

Your data remains in your repository.
