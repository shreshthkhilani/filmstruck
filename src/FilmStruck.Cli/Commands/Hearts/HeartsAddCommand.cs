using System.ComponentModel;
using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands.Hearts;

public class HeartsAddCommand : AsyncCommand<HeartsAddCommand.Settings>
{
    public class Settings : HeartsSettings
    {
        [CommandOption("--tmdb-id <ID>")]
        [Description("TMDB movie ID to add to hearts")]
        public int? TmdbId { get; set; }

        public override ValidationResult Validate()
        {
            if (TmdbId.HasValue && TmdbId.Value <= 0)
            {
                return ValidationResult.Error("TMDB ID must be a positive integer");
            }

            return ValidationResult.Success();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var csvService = new CsvService();

        // Load existing data
        var approvedFilms = await csvService.LoadApprovedFilmsAsync();
        var hearts = await csvService.LoadHeartsAsync();

        if (approvedFilms.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No films in your log yet.[/] Add some films first with [green]filmstruck add[/].");
            return 1;
        }

        int tmdbId;

        if (settings.TmdbId.HasValue)
        {
            // Direct mode: use provided TMDB ID
            tmdbId = settings.TmdbId.Value;

            if (!approvedFilms.ContainsKey(tmdbId))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Film with TMDB ID {tmdbId} is not in your log.");
                AnsiConsole.MarkupLine("[dim]You can only heart films you've already logged.[/]");
                return 1;
            }

            if (hearts.Contains(tmdbId))
            {
                var film = approvedFilms[tmdbId];
                AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(film.Title)}[/] is already in your favorites.");
                return 0;
            }
        }
        else
        {
            // Interactive mode: select from logged films
            var availableFilms = approvedFilms.Values
                .Where(f => !hearts.Contains(f.TmdbId))
                .OrderBy(f => f.Title)
                .ToList();

            if (availableFilms.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]All your logged films are already in favorites![/]");
                return 0;
            }

            var choices = availableFilms
                .Select(f => $"{f.Title} ({f.ReleaseYear}) - {f.Director ?? "Unknown director"}")
                .Concat(new[] { "<Cancel>" })
                .ToList();

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a film to add to [red]favorites[/]:")
                    .PageSize(15)
                    .AddChoices(choices));

            if (selected == "<Cancel>")
            {
                AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
                return 0;
            }

            var selectedIndex = choices.IndexOf(selected);
            tmdbId = availableFilms[selectedIndex].TmdbId;
        }

        // Add to hearts
        hearts.Add(tmdbId);
        await csvService.WriteHeartsAsync(hearts);

        var addedFilm = approvedFilms[tmdbId];
        AnsiConsole.MarkupLine($"[red]♥[/] Added [green]{Markup.Escape(addedFilm.Title)}[/] to favorites.");

        return 0;
    }
}
