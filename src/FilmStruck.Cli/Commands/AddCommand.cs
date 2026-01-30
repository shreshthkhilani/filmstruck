using System.ComponentModel;
using System.Globalization;
using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class AddCommand : AsyncCommand<AddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-t|--title <TITLE>")]
        [Description("Film title for TMDB search")]
        public string? Title { get; set; }

        [CommandOption("-d|--date <DATE>")]
        [Description("Watch date in M/d/yyyy format (default: today)")]
        public string? Date { get; set; }

        [CommandOption("-l|--location <LOCATION>")]
        [Description("Where the film was watched")]
        public string? Location { get; set; }

        [CommandOption("-c|--companions <COMPANIONS>")]
        [Description("Comma-separated list of companions")]
        public string? Companions { get; set; }

        [CommandOption("--default-poster")]
        [Description("Use the default/first poster without prompting")]
        public bool DefaultPoster { get; set; }

        [CommandOption("--tmdb-id <ID>")]
        [Description("TMDB movie ID (skips search and confirmation)")]
        public int? TmdbId { get; set; }

        public override ValidationResult Validate()
        {
            if (!string.IsNullOrEmpty(Date) &&
                !DateTime.TryParseExact(Date, "M/d/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return ValidationResult.Error("Date must be in M/d/yyyy format (e.g., 1/29/2026)");
            }

            if (TmdbId.HasValue && TmdbId.Value <= 0)
            {
                return ValidationResult.Error("TMDB ID must be a positive integer");
            }

            return ValidationResult.Success();
        }
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
        var posterService = new PosterSelectionService();

        AnsiConsole.MarkupLine("[bold blue]Add a new film[/]\n");

        // Load existing data
        var films = await csvService.LoadLogAsync();
        var approvedFilms = await csvService.LoadApprovedFilmsAsync();
        var recentLocations = csvService.GetRecentLocations(films);

        // 1. Get title (from flag or prompt)
        var title = settings.Title ?? AnsiConsole.Ask<string>("Film [green]title[/]:");

        // 2. Search TMDB and select (or use --tmdb-id if provided)
        int? tmdbId;
        if (settings.TmdbId.HasValue)
        {
            // Validate the TMDB ID exists
            var (validatedFilm, error) = await AnsiConsole.Status()
                .StartAsync("Validating TMDB ID...", async ctx =>
                {
                    return await tmdbService.GetApprovedFilmAsync(settings.TmdbId.Value, title);
                });

            if (validatedFilm == null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error ?? "Invalid TMDB ID")}");
                return 1;
            }

            AnsiConsole.MarkupLine($"[dim]Using TMDB ID {settings.TmdbId.Value}:[/] {Markup.Escape(validatedFilm.Title)} ({validatedFilm.ReleaseYear})");
            tmdbId = settings.TmdbId.Value;
        }
        else
        {
            tmdbId = await SearchAndSelectFilmAsync(tmdbService, title);
            if (tmdbId == null)
            {
                AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                return 0;
            }
        }

        // 3. Get or create approved film entry
        ApprovedFilm approved;
        bool isNewFilm = !approvedFilms.ContainsKey(tmdbId.Value);

        if (isNewFilm)
        {
            var (fetchedApproved, error) = await tmdbService.GetApprovedFilmAsync(tmdbId.Value, title);
            if (fetchedApproved == null)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching film details:[/] {Markup.Escape(error ?? "Unknown error")}");
                return 1;
            }
            approved = fetchedApproved;
        }
        else
        {
            approved = approvedFilms[tmdbId.Value];
            AnsiConsole.MarkupLine($"[dim]Film already in films.csv:[/] {Markup.Escape(approved.Title)}");
        }

        // 4. Poster selection
        var posters = await AnsiConsole.Status()
            .StartAsync("Fetching posters...", async ctx =>
            {
                return await tmdbService.GetMoviePostersAsync(tmdbId.Value);
            });

        if (posters.Count > 1 && !settings.DefaultPoster)
        {
            var selectedPoster = posterService.SelectPoster(
                approved.Title,
                approved.ReleaseYear,
                tmdbId.Value,
                posters,
                approved.PosterPath);
            approved = approved with { PosterPath = selectedPoster };
        }

        approvedFilms[tmdbId.Value] = approved;
        if (isNewFilm)
        {
            AnsiConsole.MarkupLine($"[green]Added to films.csv:[/] {Markup.Escape(approved.Title)} ({approved.ReleaseYear}) - {Markup.Escape(approved.Director ?? "Unknown")}");
        }

        // 5. Get date (from flag or prompt, default: today)
        string date;
        if (!string.IsNullOrEmpty(settings.Date))
        {
            date = settings.Date;
        }
        else
        {
            var today = DateTime.Now.ToString("M/d/yyyy");
            var dateInput = AnsiConsole.Prompt(
                new TextPrompt<string>($"[green]Date[/] (M/d/yyyy):")
                    .DefaultValue(today)
                    .AllowEmpty());
            date = string.IsNullOrWhiteSpace(dateInput) ? today : dateInput;
        }

        // 6. Get location (from flag or prompt)
        string location;
        if (!string.IsNullOrEmpty(settings.Location))
        {
            location = settings.Location;
        }
        else if (recentLocations.Count > 0)
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

        // 7. Get companions (from flag or prompt, optional)
        string companions;
        if (settings.Companions != null)
        {
            companions = settings.Companions;
        }
        else
        {
            companions = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Companions[/] (comma-separated, or empty):")
                    .AllowEmpty()) ?? "";
        }

        // 8. Append to log.csv
        var newFilm = new Film(date, title, location, companions, tmdbId.Value);
        films.Add(newFilm);
        await csvService.WriteLogAsync(films);
        await csvService.WriteApprovedFilmsAsync(approvedFilms);

        // 9. Recompute stats
        var statsService = new StatsService();
        var stats = statsService.CalculateStats(films, approvedFilms);
        await statsService.WriteStatsAsync(csvService.StatsPath, stats);

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
