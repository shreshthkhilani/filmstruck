using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class AddCommand : AsyncCommand<AddCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] TMDB_API_KEY environment variable is required");
            AnsiConsole.MarkupLine("Get an API key at [link]https://www.themoviedb.org/settings/api[/]");
            return 1;
        }

        var csvService = new CsvService();
        using var tmdbService = new TmdbService(apiKey);

        AnsiConsole.MarkupLine("[bold blue]Add a new film[/]\n");

        // Load existing data
        var films = await csvService.LoadLogAsync();
        var approvedFilms = await csvService.LoadApprovedFilmsAsync();
        var recentLocations = csvService.GetRecentLocations(films);

        // 1. Prompt for title
        var title = AnsiConsole.Ask<string>("Film [green]title[/]:");

        // 2. Search TMDB and select
        var tmdbId = await SearchAndSelectFilmAsync(tmdbService, title);
        if (tmdbId == null)
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return 0;
        }

        // 3. Get or create approved film entry
        if (!approvedFilms.ContainsKey(tmdbId.Value))
        {
            var (approved, error) = await tmdbService.GetApprovedFilmAsync(tmdbId.Value, title);
            if (approved == null)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching film details:[/] {Markup.Escape(error ?? "Unknown error")}");
                return 1;
            }
            approvedFilms[tmdbId.Value] = approved;
            AnsiConsole.MarkupLine($"[green]Added to films.csv:[/] {Markup.Escape(approved.Title)} ({approved.ReleaseYear}) - {Markup.Escape(approved.Director ?? "Unknown")}");
        }
        else
        {
            var existing = approvedFilms[tmdbId.Value];
            AnsiConsole.MarkupLine($"[dim]Film already in films.csv:[/] {Markup.Escape(existing.Title)}");
        }

        // 4. Prompt for date (default: today)
        var today = DateTime.Now.ToString("M/d/yyyy");
        var dateInput = AnsiConsole.Prompt(
            new TextPrompt<string>($"[green]Date[/] (M/d/yyyy):")
                .DefaultValue(today)
                .AllowEmpty());
        var date = string.IsNullOrWhiteSpace(dateInput) ? today : dateInput;

        // 5. Prompt for location
        string location;
        if (recentLocations.Count > 0)
        {
            var locationChoices = recentLocations.Concat(new[] { "<Enter new location>" }).ToList();
            var selectedLocation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Location[/]:")
                    .PageSize(12)
                    .AddChoices(locationChoices));

            if (selectedLocation == "<Enter new location>")
            {
                location = AnsiConsole.Ask<string>("Enter [green]location[/]:");
            }
            else
            {
                location = selectedLocation;
            }
        }
        else
        {
            location = AnsiConsole.Ask<string>("Enter [green]location[/]:");
        }

        // 6. Prompt for companions (optional)
        var companions = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Companions[/] (comma-separated, or empty):")
                .AllowEmpty());

        // 7. Append to log.csv
        var newFilm = new Film(date, title, location, companions ?? "", tmdbId.Value);
        films.Add(newFilm);
        await csvService.WriteLogAsync(films);
        await csvService.WriteApprovedFilmsAsync(approvedFilms);

        AnsiConsole.MarkupLine($"\n[bold green]Added:[/] {Markup.Escape(title)} on {date} at {Markup.Escape(location)}");
        AnsiConsole.MarkupLine("[dim]Run 'node build.js' to update the site.[/]");

        return 0;
    }

    private async Task<int?> SearchAndSelectFilmAsync(TmdbService tmdbService, string title)
    {
        var searchResults = await AnsiConsole.Status()
            .StartAsync("Searching TMDB...", async ctx =>
            {
                return await tmdbService.SearchMoviesAsync(title);
            });

        if (searchResults.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results found.[/]");

            if (AnsiConsole.Confirm("Enter TMDB ID manually?"))
            {
                return await EnterManualIdAsync(tmdbService);
            }
            return null;
        }

        // Fetch director info for results
        var options = await AnsiConsole.Status()
            .StartAsync("Fetching details...", async ctx =>
            {
                return await tmdbService.GetMovieOptionsAsync(searchResults);
            });

        // Build selection list - escape special characters for display
        var choices = options
            .Select(o => FormatMovieOption(o))
            .Concat(new[] { "<Enter TMDB ID manually>", "<Cancel>" })
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select the correct film:")
                .PageSize(10)
                .AddChoices(choices));

        if (selected == "<Cancel>")
            return null;

        if (selected == "<Enter TMDB ID manually>")
            return await EnterManualIdAsync(tmdbService);

        var selectedIndex = choices.IndexOf(selected);
        return options[selectedIndex].Movie.Id;
    }

    private async Task<int?> EnterManualIdAsync(TmdbService tmdbService)
    {
        var idInput = AnsiConsole.Ask<string>("Enter [green]TMDB ID[/]:");
        if (!int.TryParse(idInput, out int manualId))
        {
            AnsiConsole.MarkupLine("[red]Invalid ID[/]");
            return null;
        }

        var (approved, error) = await AnsiConsole.Status()
            .StartAsync("Fetching movie details...", async ctx =>
            {
                return await tmdbService.GetApprovedFilmAsync(manualId, "");
            });

        if (approved == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error ?? "Unknown error")}");
            return null;
        }

        AnsiConsole.MarkupLine($"\n[bold]{Markup.Escape(approved.Title)}[/] ({approved.ReleaseYear}) - {Markup.Escape(approved.Director ?? "Unknown director")}");

        if (AnsiConsole.Confirm("Is this correct?"))
            return manualId;

        return null;
    }

    private static string FormatMovieOption(MovieOption o)
    {
        return $"{o.Movie.Title} ({o.Year}) - {o.Director ?? "Unknown director"}";
    }
}
