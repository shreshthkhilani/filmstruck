# Contributing to FilmStruck

Thank you for your interest in contributing to FilmStruck!

## Development Setup

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A TMDB API key from [themoviedb.org](https://www.themoviedb.org/settings/api)
- [Docker](https://docs.docker.com/get-docker/) (for local API development)
- [Node.js](https://nodejs.org/) (for CDK infrastructure, optional)
- [AWS CLI](https://aws.amazon.com/cli/) (for local DynamoDB seeding and deployments, optional)

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
make reinstall
```

Or manually:
```bash
cd src/FilmStruck.Cli
dotnet pack
dotnet tool install --global --add-source ./nupkg FilmStruck.Cli
```

### Development Commands

```bash
# CLI
make build      # Build the CLI
make test       # Run all tests (unit + integration)
make clean      # Clean build artifacts
make reinstall  # Reinstall CLI globally

# API
make api-build  # Build the API project
make api-run    # Run API locally (needs DynamoDB Local)
make local-up   # Start DynamoDB Local + seed test data

# Infrastructure
make infra-install        # Install CDK dependencies
make infra-synth          # Synthesize CloudFormation template
make infra-deploy-staging # Deploy to staging
make infra-deploy-prod    # Deploy to production

make help       # Show all commands
```

## Project Structure

```
filmstruck/
├── src/
│   ├── FilmStruck.Cli/
│   │   ├── Commands/           # CLI command implementations
│   │   ├── Services/           # Business logic
│   │   ├── Templates/          # HTML/CSS/JS templates (embedded)
│   │   ├── Resources/          # Init templates (embedded)
│   │   └── Program.cs          # Entry point
│   └── FilmStruck.Api/
│       ├── Models/             # DynamoDB item models and API DTOs
│       ├── Services/           # WatchLogService (DDB query + join)
│       └── Program.cs          # Minimal API entry point
├── infra/
│   ├── bin/app.ts              # CDK app entry point
│   ├── lib/filmstruck-stack.ts # CDK stack (DDB, Lambda, API GW, DNS)
│   └── config/environments.ts  # Staging/prod environment configs
├── scripts/
│   ├── test.sh                 # Integration tests
│   └── local-setup.sh          # Create DDB table + seed data locally
├── .github/
│   └── workflows/
│       └── publish.yml         # Publishes to NuGet
├── docker-compose.yml          # DynamoDB Local for development
├── docs/                       # Documentation
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

### API (FilmStruck.Api)

- `Program.cs` - Minimal API with `GET /api/{username}` route, DynamoDB client registration, Lambda hosting
- `Services/WatchLogService.cs` - Queries DynamoDB, splits items by SortKey prefix (`Log#` vs `Film#`), joins log entries with film metadata
- `Models/LogItem.cs` - DynamoDB log record model
- `Models/FilmItem.cs` - DynamoDB film metadata model
- `Models/LogResponse.cs` - API response DTOs

### Infrastructure (infra/)

- `bin/app.ts` - CDK app entry point, reads environment from context
- `lib/filmstruck-stack.ts` - CDK stack: DynamoDB table, Lambda function, API Gateway, Route 53, ACM certificate
- `config/environments.ts` - Staging and prod environment configurations

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

## Local API Development

1. **Start DynamoDB Local:**
   ```bash
   docker compose up -d
   ```

2. **Create table and seed test data:**
   ```bash
   ./scripts/local-setup.sh
   ```

3. **Run the API:**
   ```bash
   make api-run
   ```

4. **Test the endpoint:**
   ```bash
   curl http://localhost:5000/api/testuser
   ```

The API reads `AWS_ENDPOINT_URL` to connect to DynamoDB Local and `TABLE_NAME` for the table name. Both are set automatically by `make api-run`.

## Making Changes

### Adding a New Command

1. Create `src/FilmStruck.Cli/Commands/YourCommand.cs`
2. Implement `Command<YourCommand.Settings>` or `AsyncCommand<...>`
3. Register in `Program.cs`:
   ```csharp
   config.AddCommand<YourCommand>("yourcommand")
       .WithDescription("Description here");
   ```
4. **Add integration tests** in `scripts/test.sh` (see Testing section)

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

Run the integration test suite:

```bash
make test
```

The test suite:
- Dynamically creates a test site with sample data
- Runs `enrich`, `calculate`, `add`, and `build` commands
- Validates expected outputs (files created, data updated)
- Cleans up automatically

Tests requiring TMDB API access are skipped if `TMDB_API_KEY` is not set. To run the full suite:

```bash
export TMDB_API_KEY="your-api-key"
make test
```

### Adding Integration Tests

**All new commands must have integration tests.** When adding a command:

1. Add a test section in `scripts/test.sh`
2. Test both success and expected outputs
3. Use non-interactive flags for CI compatibility
4. Verify file changes where applicable

## Submitting Changes

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally
5. Submit a pull request

## Releasing to NuGet

### First-Time Setup

1. **Get a NuGet API key:**
   - Go to [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys)
   - Create a new API key with "Push" scope for "FilmStruck.Cli" package
   - Copy the key (you won't see it again)

2. **Add the secret to GitHub:**
   - Go to your repo's Settings > Secrets and variables > Actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: paste your NuGet API key

### Publishing a Release

1. **Update the version** in `src/FilmStruck.Cli/FilmStruck.Cli.csproj`:
   ```xml
   <Version>1.0.1</Version>
   ```

2. **Commit the version bump:**
   ```bash
   git add src/FilmStruck.Cli/FilmStruck.Cli.csproj
   git commit -m "Bump version to 1.0.1"
   git push
   ```

3. **Create a GitHub release:**
   - Go to Releases > "Create a new release"
   - Tag: `v1.0.1` (must match version with `v` prefix)
   - Title: `v1.0.1`
   - Describe the changes
   - Click "Publish release"

4. **Verify:** The `publish.yml` workflow will automatically:
   - Build the package
   - Push to NuGet.org
   - Check the Actions tab for status

### Manual Publishing (Alternative)

If you need to publish manually:

```bash
cd src/FilmStruck.Cli
dotnet pack --configuration Release -p:Version=1.0.1
dotnet nuget push ./nupkg/FilmStruck.Cli.1.0.1.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

## Questions?

Open an issue on GitHub if you have questions or need help.
