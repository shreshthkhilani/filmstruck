using FilmStruck.Cli.Commands;
using FilmStruck.Cli.Commands.Hearts;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("filmstruck");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Initialize a new filmstruck repository");

    config.AddCommand<AddCommand>("add")
        .WithDescription("Add a new film entry with TMDB lookup");

    config.AddCommand<EnrichCommand>("enrich")
        .WithDescription("Look up TMDB IDs for existing log entries");

    config.AddCommand<CalculateCommand>("calculate")
        .WithDescription("Calculate and write statistics to stats.csv");

    config.AddCommand<BuildCommand>("build")
        .WithDescription("Generate static site from CSV data");

    config.AddCommand<ImportLetterboxdCommand>("import-letterboxd-diary")
        .WithDescription("Import films from Letterboxd diary CSV export");

    config.AddBranch<HeartsSettings>("hearts", hearts =>
    {
        hearts.SetDescription("Manage favorite films");

        hearts.AddCommand<HeartsAddCommand>("add")
            .WithDescription("Add a film to favorites");
    });
});

return await app.RunAsync(args);
