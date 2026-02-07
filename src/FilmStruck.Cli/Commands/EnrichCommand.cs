using System.ComponentModel;
using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class EnrichCommand : AsyncCommand<EnrichCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--default-poster")]
        [Description("Use the default poster without prompting")]
        public bool DefaultPoster { get; set; }
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

        AnsiConsole.MarkupLine($"[dim]Reading from:[/] {Markup.Escape(csvService.LogPath)}\n");

        var films = await csvService.LoadLogAsync();
        var approvedFilms = await csvService.LoadApprovedFilmsAsync();

        AnsiConsole.MarkupLine($"Found [bold]{films.Count}[/] films total");
        AnsiConsole.MarkupLine($"Loaded [bold]{approvedFilms.Count}[/] approved films\n");

        // Sync films that have tmdbId but are missing from films.csv
        await SyncMissingApprovedFilmsAsync(films, approvedFilms, tmdbService, csvService, posterService, settings.DefaultPoster);

        // Process films without tmdbId
        var filmsToProcess = films.Where(f => f.TmdbId == null).ToList();
        AnsiConsole.MarkupLine($"\n[bold]{filmsToProcess.Count}[/] films need TMDB lookup\n");

        for (int i = 0; i < filmsToProcess.Count; i++)
        {
            var film = filmsToProcess[i];
            AnsiConsole.MarkupLine($"[bold][[{i + 1}/{filmsToProcess.Count}]][/] {Markup.Escape(film.Title)}");

            try
            {
                var searchResults = await tmdbService.SearchMoviesAsync(film.Title);

                if (searchResults.Count == 0)
                {
                    AnsiConsole.MarkupLine("  [yellow]No results found.[/]");
                    if (!AnsiConsole.Confirm("  Enter TMDB ID manually?", false))
                        continue;

                    var manualResult = await ProcessManualIdAsync(film, tmdbService, posterService);
                    if (manualResult != null)
                    {
                        film.TmdbId = manualResult.TmdbId;
                        approvedFilms[manualResult.TmdbId] = manualResult;
                        await SaveAsync(films, approvedFilms, csvService);
                        AnsiConsole.MarkupLine($"  [green]Saved:[/] {Markup.Escape(manualResult.Title)} ({manualResult.ReleaseYear}) - {Markup.Escape(manualResult.Director ?? "Unknown")}\n");
                    }
                    continue;
                }

                // Fetch directors for results
                var options = await tmdbService.GetMovieOptionsAsync(searchResults);

                // Build selection list
                var choices = options
                    .Select(o => FormatMovieOption(o))
                    .Concat(new[] { "<Enter TMDB ID manually>", "<Skip>", "<Quit>" })
                    .ToList();

                var selected = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("  Select:")
                        .PageSize(10)
                        .AddChoices(choices));

                if (selected == "<Quit>")
                {
                    AnsiConsole.MarkupLine("\n[yellow]Quitting...[/]");
                    break;
                }

                if (selected == "<Skip>")
                {
                    AnsiConsole.MarkupLine("  [dim]Skipped[/]\n");
                    continue;
                }

                if (selected == "<Enter TMDB ID manually>")
                {
                    var manualResult = await ProcessManualIdAsync(film, tmdbService, posterService);
                    if (manualResult != null)
                    {
                        film.TmdbId = manualResult.TmdbId;
                        approvedFilms[manualResult.TmdbId] = manualResult;
                        await SaveAsync(films, approvedFilms, csvService);
                        AnsiConsole.MarkupLine($"  [green]Saved:[/] {Markup.Escape(manualResult.Title)} ({manualResult.ReleaseYear}) - {Markup.Escape(manualResult.Director ?? "Unknown")}\n");
                    }
                    continue;
                }

                // Regular selection
                var selectedIndex = choices.IndexOf(selected);
                var selectedOption = options[selectedIndex];

                film.TmdbId = selectedOption.Movie.Id;

                var approved = new ApprovedFilm(
                    selectedOption.Movie.Id,
                    selectedOption.Movie.Title ?? film.Title,
                    selectedOption.Director,
                    selectedOption.Year,
                    selectedOption.Movie.OriginalLanguage,
                    selectedOption.Movie.PosterPath
                );

                // Poster selection
                if (!settings.DefaultPoster)
                {
                    var posters = await tmdbService.GetMoviePostersAsync(selectedOption.Movie.Id);
                    if (posters.Count > 1)
                    {
                        var selectedPoster = posterService.SelectPoster(
                            approved.Title,
                            approved.ReleaseYear,
                            selectedOption.Movie.Id,
                            posters,
                            approved.PosterPath);
                        approved = approved with { PosterPath = selectedPoster };
                    }
                }

                approvedFilms[selectedOption.Movie.Id] = approved;

                await SaveAsync(films, approvedFilms, csvService);
                AnsiConsole.MarkupLine($"  [green]Saved:[/] {Markup.Escape(approved.Title)} ({approved.ReleaseYear}) - {Markup.Escape(approved.Director ?? "Unknown")}\n");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]ERROR:[/] {Markup.Escape(ex.Message)}\n");
            }
        }

        AnsiConsole.MarkupLine("\n[bold green]Done![/]");
        return 0;
    }

    private async Task SyncMissingApprovedFilmsAsync(
        List<Film> films,
        Dictionary<int, ApprovedFilm> approvedFilms,
        TmdbService tmdbService,
        CsvService csvService,
        PosterSelectionService posterService,
        bool defaultPoster)
    {
        var filmsWithIdButNotApproved = films
            .Where(f => f.TmdbId.HasValue && !approvedFilms.ContainsKey(f.TmdbId.Value))
            .ToList();

        if (filmsWithIdButNotApproved.Count == 0)
            return;

        AnsiConsole.MarkupLine($"[yellow]Syncing {filmsWithIdButNotApproved.Count} films to films.csv...[/]\n");

        for (int i = 0; i < filmsWithIdButNotApproved.Count; i++)
        {
            var film = filmsWithIdButNotApproved[i];
            AnsiConsole.MarkupLine($"[bold][[{i + 1}/{filmsWithIdButNotApproved.Count}]][/] {Markup.Escape(film.Title)}");

            var (approved, error) = await tmdbService.GetApprovedFilmAsync(film.TmdbId!.Value, film.Title);
            if (approved != null)
            {
                // Poster selection
                if (!defaultPoster)
                {
                    var posters = await tmdbService.GetMoviePostersAsync(film.TmdbId.Value);
                    if (posters.Count > 1)
                    {
                        var selectedPoster = posterService.SelectPoster(
                            approved.Title,
                            approved.ReleaseYear,
                            film.TmdbId.Value,
                            posters,
                            approved.PosterPath);
                        approved = approved with { PosterPath = selectedPoster };
                    }
                }

                approvedFilms[film.TmdbId.Value] = approved;
                await csvService.WriteApprovedFilmsAsync(approvedFilms);
                AnsiConsole.MarkupLine($"  [green]Saved:[/] {Markup.Escape(approved.Title)} ({approved.ReleaseYear ?? "?"}) - {Markup.Escape(approved.Director ?? "Unknown")}\n");
            }
            else
            {
                AnsiConsole.MarkupLine($"  [red]Error:[/] {Markup.Escape(error ?? "Unknown error")}\n");
            }
            await Task.Delay(50);
        }

        AnsiConsole.MarkupLine("[green]Sync complete.[/]");
    }

    private async Task<ApprovedFilm?> ProcessManualIdAsync(Film film, TmdbService tmdbService, PosterSelectionService posterService)
    {
        var idInput = AnsiConsole.Ask<string>("  Enter TMDB ID:");
        if (!int.TryParse(idInput, out int manualId))
        {
            AnsiConsole.MarkupLine("  [red]Invalid ID[/]\n");
            return null;
        }

        var (approved, error) = await tmdbService.GetApprovedFilmAsync(manualId, film.Title);
        if (approved == null)
        {
            AnsiConsole.MarkupLine($"  [red]Error:[/] {Markup.Escape(error ?? "Unknown error")}\n");
            return null;
        }

        AnsiConsole.MarkupLine($"\n  [bold]{Markup.Escape(approved.Title)}[/] ({approved.ReleaseYear}) - {Markup.Escape(approved.Director ?? "Unknown director")}");

        if (AnsiConsole.Confirm("  Confirm?"))
        {
            // Poster selection
            var posters = await tmdbService.GetMoviePostersAsync(manualId);
            if (posters.Count > 1)
            {
                var selectedPoster = posterService.SelectPoster(
                    approved.Title,
                    approved.ReleaseYear,
                    manualId,
                    posters,
                    approved.PosterPath);
                approved = approved with { PosterPath = selectedPoster };
            }
            return approved;
        }

        AnsiConsole.MarkupLine("  [dim]Cancelled[/]\n");
        return null;
    }

    private async Task SaveAsync(List<Film> films, Dictionary<int, ApprovedFilm> approvedFilms, CsvService csvService)
    {
        await csvService.WriteLogAsync(films);
        await csvService.WriteApprovedFilmsAsync(approvedFilms);
    }

    private static string FormatMovieOption(MovieOption o)
    {
        return $"{o.Movie.Title} ({o.Year}) - {o.Director ?? "Unknown director"}";
    }
}
