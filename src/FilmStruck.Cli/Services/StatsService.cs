using System.Globalization;
using System.Text;

namespace FilmStruck.Cli.Services;

public class StatsService
{
    public Dictionary<string, Dictionary<string, int>> CalculateStats(
        List<Film> log,
        Dictionary<int, ApprovedFilm> films)
    {
        var stats = new Dictionary<string, Dictionary<string, int>>
        {
            ["watch_year"] = new(),
            ["director"] = new(),
            ["language"] = new(),
            ["companion"] = new(),
            ["location"] = new(),
            ["release_decade"] = new()
        };

        // Process each log entry
        foreach (var entry in log)
        {
            // watch_year: Parse date and extract year
            if (TryParseWatchYear(entry.Date, out int watchYear))
            {
                var yearKey = watchYear.ToString();
                stats["watch_year"][yearKey] = stats["watch_year"].GetValueOrDefault(yearKey) + 1;
            }
            stats["watch_year"]["ALL_TIME"] = stats["watch_year"].GetValueOrDefault("ALL_TIME") + 1;

            // companion: Split by comma and count each
            if (!string.IsNullOrWhiteSpace(entry.Companions))
            {
                var companions = entry.Companions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (var companion in companions)
                {
                    stats["companion"][companion] = stats["companion"].GetValueOrDefault(companion) + 1;
                }
            }

            // location: Count per location
            if (!string.IsNullOrWhiteSpace(entry.Location))
            {
                stats["location"][entry.Location] = stats["location"].GetValueOrDefault(entry.Location) + 1;
            }

            // Join with films for director, language, release_decade
            if (entry.TmdbId.HasValue && films.TryGetValue(entry.TmdbId.Value, out var film))
            {
                // director: Split by comma and count each
                if (!string.IsNullOrWhiteSpace(film.Director))
                {
                    var directors = film.Director.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    foreach (var director in directors)
                    {
                        stats["director"][director] = stats["director"].GetValueOrDefault(director) + 1;
                    }
                }

                // language: Count per language code
                if (!string.IsNullOrWhiteSpace(film.Language))
                {
                    stats["language"][film.Language] = stats["language"].GetValueOrDefault(film.Language) + 1;
                }

                // release_decade: Compute decade from releaseYear
                if (!string.IsNullOrWhiteSpace(film.ReleaseYear) && int.TryParse(film.ReleaseYear, out int releaseYear))
                {
                    var decade = (releaseYear / 10) * 10;
                    var decadeKey = $"{decade}s";
                    stats["release_decade"][decadeKey] = stats["release_decade"].GetValueOrDefault(decadeKey) + 1;
                }
            }
        }

        return stats;
    }

    public async Task WriteStatsAsync(string path, Dictionary<string, Dictionary<string, int>> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("stat,key,value");

        // Write stats in a consistent order
        var statOrder = new[] { "watch_year", "director", "language", "companion", "location", "release_decade" };

        foreach (var statType in statOrder)
        {
            if (!stats.TryGetValue(statType, out var values))
                continue;

            // Sort: for watch_year, put ALL_TIME first then years descending
            // For others, sort by count descending
            IEnumerable<KeyValuePair<string, int>> sorted;
            if (statType == "watch_year")
            {
                sorted = values.OrderByDescending(kv => kv.Key == "ALL_TIME" ? int.MaxValue : int.Parse(kv.Key));
            }
            else
            {
                sorted = values.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key);
            }

            foreach (var kv in sorted)
            {
                var key = kv.Key.Contains(',') ? $"\"{kv.Key}\"" : kv.Key;
                sb.AppendLine($"{statType},{key},{kv.Value}");
            }
        }

        await File.WriteAllTextAsync(path, sb.ToString());
    }

    private static bool TryParseWatchYear(string date, out int year)
    {
        year = 0;
        if (string.IsNullOrWhiteSpace(date))
            return false;

        // Try parsing M/d/yyyy format
        if (DateTime.TryParseExact(date, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            year = dt.Year;
            return true;
        }

        // Fallback: try other common formats
        if (DateTime.TryParse(date, out dt))
        {
            year = dt.Year;
            return true;
        }

        return false;
    }
}
