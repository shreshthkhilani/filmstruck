using System.Reflection;
using System.Text.Json;

namespace FilmStruck.Cli.Services;

public class SiteGeneratorService
{
    private static readonly Assembly Assembly = typeof(SiteGeneratorService).Assembly;
    private static readonly string ResourcePrefix = "FilmStruck.Cli.Templates.";

    public string GenerateHtml(List<WatchedFilm> films, List<CompanionCount> companions, HashSet<int> hearts, FilmStruckConfig config)
    {
        var template = LoadTemplate("index.html");
        var styles = LoadTemplate("styles.css");
        var appJs = LoadTemplate("app.js");

        var filmsJson = JsonSerializer.Serialize(films);
        var companionsJson = JsonSerializer.Serialize(companions);
        var heartsJson = JsonSerializer.Serialize(hearts.OrderBy(id => id).ToList());

        return template
            .Replace("{{USERNAME}}", config.Username)
            .Replace("{{STYLES}}", styles)
            .Replace("{{FILMS_DATA}}", filmsJson)
            .Replace("{{COMPANIONS_DATA}}", companionsJson)
            .Replace("{{HEARTS_DATA}}", heartsJson)
            .Replace("{{APP_JS}}", appJs);
    }

    private static string LoadTemplate(string name)
    {
        var resourceName = ResourcePrefix + name;
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Template resource '{resourceName}' not found. Available resources: {string.Join(", ", Assembly.GetManifestResourceNames())}");
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
