using System.Net;
using System.Text;
using System.Text.Json;
using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class BackfillRuntimesCommand : AsyncCommand<BackfillRuntimesCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] TMDB_API_KEY environment variable is required");
            return 1;
        }

        var csv = new CsvService();
        using var tmdb = new TmdbService(apiKey);
        try
        {
            var updated = await csv.BackfillRuntimesAsync(async id =>
            {
                await Task.Delay(50); // Pace requests for large logs
                try
                {
                    var movie = await tmdb.GetMovieDetailsAsync(id);
                    if (movie?.RuntimeMinutes is > 0)
                        return movie.RuntimeMinutes;

                    AnsiConsole.MarkupLine($"[yellow]TMDB ID {id} has no usable runtime.[/]");
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    AnsiConsole.MarkupLine($"[yellow]TMDB ID {id} was not found.[/]");
                }
                return null;
            });

            AnsiConsole.MarkupLine($"[green]Updated runtimes for {updated} films.[/]");
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            HttpRequestException or TaskCanceledException or JsonException or InvalidDataException or DecoderFallbackException)
        {
            AnsiConsole.MarkupLine($"[red]Backfill stopped:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
