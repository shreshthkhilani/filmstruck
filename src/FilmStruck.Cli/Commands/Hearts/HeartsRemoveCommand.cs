using System.ComponentModel;
using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands.Hearts;

public class HeartsRemoveCommand : AsyncCommand<HeartsRemoveCommand.Settings>
{
    public class Settings : HeartsSettings
    {
        [CommandOption("--tmdb-id <ID>")]
        [Description("TMDB movie ID to remove from hearts")]
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

        if (hearts.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No films in your favorites yet.[/]");
            return 0;
        }

        int tmdbId;

        if (settings.TmdbId.HasValue)
        {
            // Direct mode: use provided TMDB ID
            tmdbId = settings.TmdbId.Value;

            if (!hearts.Contains(tmdbId))
            {
                AnsiConsole.MarkupLine($"[yellow]Film with TMDB ID {tmdbId} is not in your favorites.[/]");
                return 0;
            }
        }
        else
        {
            // Interactive mode: select from hearted films
            var heartedFilms = hearts
                .Where(id => approvedFilms.ContainsKey(id))
                .Select(id => approvedFilms[id])
                .OrderBy(f => f.Title)
                .ToList();

            if (heartedFilms.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No films in your favorites yet.[/]");
                return 0;
            }

            var choices = heartedFilms
                .Select(f => $"{f.Title} ({f.ReleaseYear}) - {f.Director ?? "Unknown director"}")
                .Concat(new[] { "<Cancel>" })
                .ToList();

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a film to remove from [red]favorites[/]:")
                    .PageSize(15)
                    .AddChoices(choices));

            if (selected == "<Cancel>")
            {
                AnsiConsole.MarkupLine("[dim]Cancelled.[/]");
                return 0;
            }

            var selectedIndex = choices.IndexOf(selected);
            tmdbId = heartedFilms[selectedIndex].TmdbId;
        }

        // Remove from hearts
        hearts.Remove(tmdbId);
        await csvService.WriteHeartsAsync(hearts);

        var removedFilm = approvedFilms.GetValueOrDefault(tmdbId);
        var filmName = removedFilm?.Title ?? $"TMDB ID {tmdbId}";
        AnsiConsole.MarkupLine($"Removed [green]{Markup.Escape(filmName)}[/] from favorites.");

        return 0;
    }
}
