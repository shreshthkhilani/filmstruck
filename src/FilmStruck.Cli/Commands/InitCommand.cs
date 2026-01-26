using System.ComponentModel;
using System.Reflection;
using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class InitCommand : Command<InitCommand.Settings>
{
    private static readonly Assembly Assembly = typeof(InitCommand).Assembly;
    private static readonly string ResourcePrefix = "FilmStruck.Cli.Resources.";

    public class Settings : CommandSettings
    {
        [CommandOption("-u|--username <USERNAME>")]
        [Description("Your username displayed on the site")]
        public string? Username { get; set; }

        [CommandOption("-f|--force")]
        [Description("Overwrite existing files")]
        public bool Force { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var currentDir = Directory.GetCurrentDirectory();

        AnsiConsole.MarkupLine("[bold blue]Initializing FilmStruck repository...[/]\n");

        // Get username
        var username = settings.Username;
        if (string.IsNullOrWhiteSpace(username))
        {
            username = AnsiConsole.Ask<string>("Enter your [green]username[/]:");
        }

        // Check for existing files
        var configPath = Path.Combine(currentDir, "filmstruck.json");
        if (File.Exists(configPath) && !settings.Force)
        {
            AnsiConsole.MarkupLine("[yellow]filmstruck.json already exists. Use --force to overwrite.[/]");
            return 1;
        }

        // Create directories
        var dataDir = Path.Combine(currentDir, "data");
        var workflowDir = Path.Combine(currentDir, ".github", "workflows");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(workflowDir);

        // Create filmstruck.json
        var configService = new ConfigService();
        var config = new FilmStruckConfig
        {
            Username = username,
            SiteTitle = "filmstruck"
        };
        configService.SaveConfig(config, currentDir);
        AnsiConsole.MarkupLine("[green]+[/] filmstruck.json");

        // Create data/log.csv
        var logPath = Path.Combine(dataDir, "log.csv");
        if (!File.Exists(logPath) || settings.Force)
        {
            File.WriteAllText(logPath, "date,title,location,companions,tmdbId\n");
            AnsiConsole.MarkupLine("[green]+[/] data/log.csv");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]  data/log.csv (exists)[/]");
        }

        // Create data/films.csv
        var filmsPath = Path.Combine(dataDir, "films.csv");
        if (!File.Exists(filmsPath) || settings.Force)
        {
            File.WriteAllText(filmsPath, "tmdbId,title,director,releaseYear,language,posterPath\n");
            AnsiConsole.MarkupLine("[green]+[/] data/films.csv");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]  data/films.csv (exists)[/]");
        }

        // Create .github/workflows/deploy.yml
        var deployPath = Path.Combine(workflowDir, "deploy.yml");
        if (!File.Exists(deployPath) || settings.Force)
        {
            var deployContent = LoadResource("deploy.yml");
            File.WriteAllText(deployPath, deployContent);
            AnsiConsole.MarkupLine("[green]+[/] .github/workflows/deploy.yml");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]  .github/workflows/deploy.yml (exists)[/]");
        }

        // Create .gitignore
        var gitignorePath = Path.Combine(currentDir, ".gitignore");
        if (!File.Exists(gitignorePath) || settings.Force)
        {
            var gitignoreContent = LoadResource("gitignore");
            File.WriteAllText(gitignorePath, gitignoreContent);
            AnsiConsole.MarkupLine("[green]+[/] .gitignore");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]  .gitignore (exists)[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Repository initialized![/]\n");
        AnsiConsole.MarkupLine("Next steps:");
        AnsiConsole.MarkupLine("  1. Set your TMDB API key: [dim]export TMDB_API_KEY=\"your-key\"[/]");
        AnsiConsole.MarkupLine("  2. Add your first film: [dim]filmstruck add[/]");
        AnsiConsole.MarkupLine("  3. Build the site: [dim]filmstruck build[/]");
        AnsiConsole.MarkupLine("  4. Push to GitHub to deploy");

        return 0;
    }

    private static string LoadResource(string name)
    {
        var resourceName = ResourcePrefix + name;
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Resource '{resourceName}' not found. Available: {string.Join(", ", Assembly.GetManifestResourceNames())}");
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
