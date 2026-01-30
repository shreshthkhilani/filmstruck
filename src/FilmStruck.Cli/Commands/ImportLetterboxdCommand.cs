using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Globalization;

namespace FilmStruck.Cli.Commands;

public class ImportLetterboxdCommand : AsyncCommand<ImportLetterboxdCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<FILE>")]
        [Description("Path to Letterboxd diary CSV export")]
        public required string FilePath { get; set; }
    }

    private record LetterboxdEntry(string Title, string Year, string WatchedDate);

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Validate TMDB API key
        var apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] TMDB_API_KEY environment variable is required");
            return 1;
        }

        // Validate file exists
        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {settings.FilePath}");
            return 1;
        }

        // Initialize services
        var csvService = new CsvService();
        using var tmdbService = new TmdbService(apiKey);
        var posterService = new PosterSelectionService();

        // Load existing data
        var films = await csvService.LoadLogAsync();
        var approvedFilms = await csvService.LoadApprovedFilmsAsync();

        // Build set of existing (title, date) for duplicate detection
        var existing = films
            .Where(f => f.TmdbId.HasValue)
            .Select(f => (f.Title.ToLowerInvariant(), f.Date))
            .ToHashSet();

        // Parse Letterboxd CSV
        var entries = ParseLetterboxdCsv(settings.FilePath);
        AnsiConsole.MarkupLine($"Found [bold]{entries.Count}[/] entries in Letterboxd CSV\n");

        int added = 0, skipped = 0;

        foreach (var entry in entries)
        {
            var convertedDate = ConvertDate(entry.WatchedDate);
            var display = $"{entry.Title} ({entry.Year})";

            // Check for duplicate
            if (existing.Contains((entry.Title.ToLowerInvariant(), convertedDate)))
            {
                AnsiConsole.MarkupLine($"[dim]SKIP:[/] {Markup.Escape(display)} on {convertedDate} already in log");
                skipped++;
                continue;
            }

            AnsiConsole.MarkupLine($"\n[bold]{Markup.Escape(display)}[/] - {convertedDate}");

            // Prompt to add
            if (!AnsiConsole.Confirm("Add this film?", defaultValue: true))
            {
                AnsiConsole.MarkupLine("[yellow]Skipped[/]");
                skipped++;
                continue;
            }

            // Search TMDB
            var tmdbId = await SearchAndSelectFilmAsync(tmdbService, entry.Title);
            if (tmdbId == null)
            {
                AnsiConsole.MarkupLine("[yellow]Skipped (no TMDB match)[/]");
                skipped++;
                continue;
            }

            // Get or fetch ApprovedFilm
            ApprovedFilm approved;
            if (!approvedFilms.ContainsKey(tmdbId.Value))
            {
                var (fetched, error) = await tmdbService.GetApprovedFilmAsync(tmdbId.Value, entry.Title);
                if (fetched == null)
                {
                    AnsiConsole.MarkupLine($"[red]Error fetching film:[/] {error}");
                    skipped++;
                    continue;
                }
                approved = fetched;

                // Poster selection if multiple available
                var posters = await tmdbService.GetMoviePostersAsync(tmdbId.Value);
                if (posters.Count > 1)
                {
                    var selectedPoster = posterService.SelectPoster(
                        approved.Title, approved.ReleaseYear, tmdbId.Value, posters, approved.PosterPath);
                    approved = approved with { PosterPath = selectedPoster };
                }

                approvedFilms[tmdbId.Value] = approved;
            }
            else
            {
                approved = approvedFilms[tmdbId.Value];
                AnsiConsole.MarkupLine($"[dim]Using cached:[/] {Markup.Escape(approved.Title)}");
            }

            // Prompt for location
            var location = PromptForLocation(csvService, films);

            // Prompt for companions
            var companions = AnsiConsole.Prompt(
                new TextPrompt<string>("Companions (comma-separated):")
                    .AllowEmpty());

            // Create film entry
            var newFilm = new Film(convertedDate, approved.Title, location, companions, tmdbId.Value);
            films.Add(newFilm);
            existing.Add((approved.Title.ToLowerInvariant(), convertedDate));
            added++;

            AnsiConsole.MarkupLine("[green]Added![/]");
        }

        // Save all data
        await csvService.WriteLogAsync(films);
        await csvService.WriteApprovedFilmsAsync(approvedFilms);

        // Recalculate stats
        var statsService = new StatsService();
        var stats = statsService.CalculateStats(films, approvedFilms);
        await statsService.WriteStatsAsync(csvService.StatsPath, stats);

        AnsiConsole.MarkupLine($"\n[bold green]Import complete![/] Added: {added}, Skipped: {skipped}");
        return 0;
    }

    private static List<LetterboxdEntry> ParseLetterboxdCsv(string filePath)
    {
        var entries = new List<LetterboxdEntry>();
        using var reader = new StreamReader(filePath);

        // Skip header
        var header = reader.ReadLine();
        if (header == null) return entries;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            if (fields.Count < 8) continue;

            var title = fields[1];      // Name
            var year = fields[2];       // Year
            var watchedDate = fields[7]; // Watched Date

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(watchedDate))
            {
                entries.Add(new LetterboxdEntry(title, year, watchedDate));
            }
        }
        return entries;
    }

    private static List<string> ParseCsvLine(string line)
    {
        // Handle quoted fields with commas
        var fields = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        fields.Add(current.Trim());
        return fields;
    }

    private static string ConvertDate(string letterboxdDate)
    {
        // YYYY-MM-DD → M/d/yyyy
        if (DateTime.TryParseExact(letterboxdDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return $"{dt.Month}/{dt.Day}/{dt.Year}";
        }
        return letterboxdDate; // Fallback
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
            AnsiConsole.MarkupLine("[yellow]No TMDB results found[/]");
            if (AnsiConsole.Confirm("Enter TMDB ID manually?", defaultValue: false))
            {
                return AnsiConsole.Ask<int>("TMDB ID:");
            }
            return null;
        }

        var options = await AnsiConsole.Status()
            .StartAsync("Fetching details...", async ctx =>
            {
                return await tmdbService.GetMovieOptionsAsync(searchResults);
            });

        var choices = options
            .Select(o => $"{o.Movie.Title} ({o.Year}) - {o.Director ?? "Unknown"}")
            .Concat(new[] { "<Enter TMDB ID manually>", "<Skip>" })
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select the correct film:")
                .PageSize(10)
                .AddChoices(choices));

        if (selected == "<Skip>")
            return null;
        if (selected == "<Enter TMDB ID manually>")
            return AnsiConsole.Ask<int>("TMDB ID:");

        var idx = choices.IndexOf(selected);
        return options[idx].Movie.Id;
    }

    private static string PromptForLocation(CsvService csvService, List<Film> films)
    {
        var recent = csvService.GetRecentLocations(films);
        if (recent.Count > 0)
        {
            var choices = recent.Concat(new[] { "<Enter new>" }).ToList();
            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Location:")
                    .AddChoices(choices));

            if (selected != "<Enter new>")
                return selected;
        }
        return AnsiConsole.Ask<string>("Location:");
    }
}
