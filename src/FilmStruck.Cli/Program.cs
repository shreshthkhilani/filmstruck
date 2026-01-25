using FilmStruck.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("filmstruck");

    config.AddCommand<AddCommand>("add")
        .WithDescription("Add a new film entry with TMDB lookup");

    config.AddCommand<EnrichCommand>("enrich")
        .WithDescription("Look up TMDB IDs for existing log entries");
});

return await app.RunAsync(args);
