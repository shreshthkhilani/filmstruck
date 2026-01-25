# FilmStruck: Transform to Reusable Tool

## Goal

Transform FilmStruck from a personal project into a reusable CLI tool that anyone can install and use to track their film watching and deploy their own static site.

## Key Requirements

1. Remove hardcoded "s4s" username - make it configurable
2. Publish CLI to NuGet.org for easy installation
3. Users should NOT need CLI source code in their deployment repo
4. Add `filmstruck init` command to bootstrap new repos
5. Write comprehensive documentation

## Architecture Overview

### User Experience Flow

```
1. Install CLI:     dotnet tool install --global FilmStruck.Cli
2. Create repo:     mkdir my-films && cd my-films
3. Initialize:      filmstruck init --username alice
4. Add films:       filmstruck add
5. Deploy:          git push (GitHub Actions handles the rest)
```

### Minimal User Repo Structure

```
my-film-log/
├── data/
│   ├── log.csv              # Watch log
│   └── films.csv            # TMDB metadata
├── .github/
│   └── workflows/
│       └── deploy.yml       # Installs CLI from NuGet, runs build
├── filmstruck.json          # Configuration (username, etc.)
└── favicon.png              # Optional
```

---

## Implementation Plan

### Phase 1: Configuration System

**New file: `src/FilmStruck.Cli/Services/ConfigService.cs`**

```csharp
public class FilmStruckConfig
{
    public string Username { get; set; } = "user";
    public string SiteTitle { get; set; } = "filmstruck";
}

public class ConfigService
{
    public FilmStruckConfig LoadConfig(string repoRoot);
    public void SaveConfig(FilmStruckConfig config, string repoRoot);
}
```

**Modify templates to use `{{USERNAME}}` placeholder:**

- `src/FilmStruck.Cli/Templates/index.html` - Replace `s4s` with `{{USERNAME}}`
- `src/FilmStruck.Cli/Templates/app.js` - Add `const USERNAME = '{{USERNAME}}';` and use it

**Modify `SiteGeneratorService.cs`:**

- Accept config parameter
- Replace `{{USERNAME}}` placeholder in output

**Modify `BuildCommand.cs`:**

- Load config via ConfigService
- Pass config to SiteGeneratorService

### Phase 2: Init Command

**New file: `src/FilmStruck.Cli/Commands/InitCommand.cs`**

Creates:
- `filmstruck.json` with username
- `data/log.csv` (header only)
- `data/films.csv` (header only)
- `.github/workflows/deploy.yml` (installs from NuGet)
- `.gitignore`
- `favicon.png` (embedded resource)

**Register in `Program.cs`:**

```csharp
config.AddCommand<InitCommand>("init")
    .WithDescription("Initialize a new filmstruck repository");
```

### Phase 3: NuGet Publishing

**Modify `FilmStruck.Cli.csproj`:**

```xml
<PackageProjectUrl>https://github.com/shreshthkhilani/filmstruck</PackageProjectUrl>
<RepositoryUrl>https://github.com/shreshthkhilani/filmstruck</RepositoryUrl>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageReadmeFile>README.md</PackageReadmeFile>
<PackageTags>film;movie;tracker;tmdb;static-site</PackageTags>
```

**New file: `.github/workflows/publish.yml`**

Publishes to NuGet.org on release.

**Add: `LICENSE` file (MIT)**

### Phase 4: User Workflow Template

**Embedded resource for init command - deploy.yml:**

```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [main]

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet tool install --global FilmStruck.Cli
      - run: filmstruck build
      - run: |
          mkdir -p _site
          mv index.html _site/
          cp favicon.png _site/ 2>/dev/null || true
      - uses: actions/configure-pages@v4
      - uses: actions/upload-pages-artifact@v3
        with:
          path: _site

  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - uses: actions/deploy-pages@v4
        id: deployment
```

### Phase 5: Documentation

**Update `README.md`** - Focus on tool usage, quick start guide

**New file: `docs/TOOL_SETUP.md`** - Detailed setup instructions

**New file: `CONTRIBUTING.md`** - For CLI development

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/FilmStruck.Cli/Services/ConfigService.cs` | Load/save filmstruck.json |
| `src/FilmStruck.Cli/Commands/InitCommand.cs` | Bootstrap new repos |
| `src/FilmStruck.Cli/Resources/deploy.yml` | Template workflow for users |
| `src/FilmStruck.Cli/Resources/gitignore` | Template .gitignore |
| `.github/workflows/publish.yml` | NuGet publishing workflow |
| `LICENSE` | MIT license |
| `docs/TOOL_SETUP.md` | Detailed setup guide |

## Files to Modify

| File | Changes |
|------|---------|
| `src/FilmStruck.Cli/Templates/index.html` | Replace `s4s` → `{{USERNAME}}` |
| `src/FilmStruck.Cli/Templates/app.js` | Add USERNAME constant, replace hardcoded refs |
| `src/FilmStruck.Cli/Services/SiteGeneratorService.cs` | Accept config, replace USERNAME placeholder |
| `src/FilmStruck.Cli/Commands/BuildCommand.cs` | Load config, pass to generator |
| `src/FilmStruck.Cli/Program.cs` | Register init command |
| `src/FilmStruck.Cli/FilmStruck.Cli.csproj` | Add NuGet package metadata |
| `README.md` | Rewrite for tool usage |

---

## Verification

1. **Config system**: `filmstruck build` reads username from `filmstruck.json`
2. **Init command**: `filmstruck init --username test` creates valid structure
3. **Local NuGet**: `dotnet pack` then `dotnet tool install --global --add-source ./nupkg FilmStruck.Cli`
4. **Full flow**: Init repo → add film → build → verify username appears correctly
5. **GitHub Actions**: User repo deploys successfully with CLI installed from NuGet

---

## Repository Strategy

Keep personal data (`data/`) in same repo as example. Document clearly that:
- `src/` contains the CLI tool source
- `data/` contains example/personal film data
- Users create their own repos with just data + config
