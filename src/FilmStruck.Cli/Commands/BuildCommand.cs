using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class BuildCommand : AsyncCommand<BuildCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var csvService = new CsvService();
        var generator = new SiteGeneratorService();

        AnsiConsole.MarkupLine("[bold blue]Building static site...[/]\n");

        // Load data
        var log = await csvService.LoadLogAsync();
        var films = await csvService.LoadApprovedFilmsAsync();

        // Transform to view models
        var watchedFilms = JoinLogWithFilms(log, films);
        var companions = ExtractCompanions(log);

        AnsiConsole.MarkupLine($"[dim]Found {watchedFilms.Count} films with metadata[/]");
        AnsiConsole.MarkupLine($"[dim]Found {companions.Count} unique companions[/]");

        // Generate HTML
        var html = generator.GenerateHtml(watchedFilms, companions);

        // Write output
        var outputPath = Path.Combine(csvService.RepoRoot, "index.html");
        await File.WriteAllTextAsync(outputPath, html);

        AnsiConsole.MarkupLine($"\n[bold green]Generated:[/] {outputPath}");

        return 0;
    }

    private static List<WatchedFilm> JoinLogWithFilms(List<Film> log, Dictionary<int, ApprovedFilm> films)
    {
        return log
            .Where(entry => entry.TmdbId.HasValue && films.ContainsKey(entry.TmdbId.Value))
            .Select(entry =>
            {
                var film = films[entry.TmdbId!.Value];
                return new WatchedFilm(
                    Date: entry.Date,
                    Location: entry.Location,
                    Companions: entry.Companions,
                    TmdbId: entry.TmdbId.Value.ToString(),
                    Title: film.Title,
                    ReleaseYear: film.ReleaseYear ?? "",
                    Director: film.Director ?? "",
                    Language: film.Language ?? "",
                    PosterPath: film.PosterPath ?? ""
                );
            })
            .ToList();
    }

    private static List<CompanionCount> ExtractCompanions(List<Film> log)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in log)
        {
            if (string.IsNullOrWhiteSpace(entry.Companions)) continue;

            var companions = entry.Companions.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c));

            foreach (var companion in companions)
            {
                // Normalize to title case for display consistency
                var key = companion;
                if (!counts.TryGetValue(key, out _))
                {
                    counts[key] = 0;
                }
                counts[key]++;
            }
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new CompanionCount(kv.Key, kv.Value))
            .ToList();
    }
}
