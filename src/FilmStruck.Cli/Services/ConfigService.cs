using System.Text.Json;
using System.Text.Json.Serialization;

namespace FilmStruck.Cli.Services;

public class FilmStruckConfig
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "user";

    [JsonPropertyName("siteTitle")]
    public string SiteTitle { get; set; } = "filmstruck";
}

public class ConfigService
{
    private const string ConfigFileName = "filmstruck.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public FilmStruckConfig LoadConfig(string repoRoot)
    {
        var configPath = Path.Combine(repoRoot, ConfigFileName);

        if (!File.Exists(configPath))
        {
            return new FilmStruckConfig();
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<FilmStruckConfig>(json) ?? new FilmStruckConfig();
    }

    public void SaveConfig(FilmStruckConfig config, string repoRoot)
    {
        var configPath = Path.Combine(repoRoot, ConfigFileName);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }
}
