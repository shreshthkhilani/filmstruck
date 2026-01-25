using FilmStruck.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FilmStruck.Cli.Commands;

public class CalculateCommand : AsyncCommand<CalculateCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var csvService = new CsvService();
        var statsService = new StatsService();

        AnsiConsole.MarkupLine("[bold blue]Calculating statistics...[/]\n");

        // Load data
        var log = await csvService.LoadLogAsync();
        var films = await csvService.LoadApprovedFilmsAsync();

        // Calculate stats
        var stats = statsService.CalculateStats(log, films);

        // Write stats
        await statsService.WriteStatsAsync(csvService.StatsPath, stats);

        // Print summary
        AnsiConsole.MarkupLine($"[green]Total films watched:[/] {stats["watch_year"].GetValueOrDefault("ALL_TIME")}");

        if (stats["watch_year"].Count > 1)
        {
            AnsiConsole.MarkupLine("\n[bold]Watch years:[/]");
            foreach (var kv in stats["watch_year"].Where(kv => kv.Key != "ALL_TIME").OrderByDescending(kv => kv.Key).Take(5))
            {
                AnsiConsole.MarkupLine($"  {kv.Key}: {kv.Value}");
            }
        }

        if (stats["director"].Count > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Top directors:[/]");
            foreach (var kv in stats["director"].OrderByDescending(kv => kv.Value).Take(5))
            {
                AnsiConsole.MarkupLine($"  {Markup.Escape(kv.Key)}: {kv.Value}");
            }
        }

        if (stats["language"].Count > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Top languages:[/]");
            foreach (var kv in stats["language"].OrderByDescending(kv => kv.Value).Take(5))
            {
                AnsiConsole.MarkupLine($"  {kv.Key}: {kv.Value}");
            }
        }

        if (stats["release_decade"].Count > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Release decades:[/]");
            foreach (var kv in stats["release_decade"].OrderByDescending(kv => kv.Key).Take(5))
            {
                AnsiConsole.MarkupLine($"  {kv.Key}: {kv.Value}");
            }
        }

        AnsiConsole.MarkupLine($"\n[bold green]Stats written to:[/] {csvService.StatsPath}");

        return 0;
    }
}
