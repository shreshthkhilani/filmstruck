using System.Text;

namespace FilmStruck.Cli.Services;

public class CsvService
{
    private readonly string _repoRoot;
    public string RepoRoot => _repoRoot;
    public string LogPath { get; }
    public string FilmsPath { get; }
    public string StatsPath { get; }
    public string HeartsPath { get; }

    public CsvService()
    {
        _repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
        LogPath = Path.Combine(_repoRoot, "data", "log.csv");
        FilmsPath = Path.Combine(_repoRoot, "data", "films.csv");
        StatsPath = Path.Combine(_repoRoot, "data", "stats.csv");
        HeartsPath = Path.Combine(_repoRoot, "data", "hearts.csv");
    }

    private static string FindRepoRoot(string startDir)
    {
        var dir = startDir;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new Exception("Could not find repository root. Make sure you're running from within the filmstruck repository.");
    }

    public async Task<List<Film>> LoadLogAsync()
    {
        var lines = await File.ReadAllLinesAsync(LogPath);
        return ParseLogCsv(lines);
    }

    public async Task<Dictionary<int, ApprovedFilm>> LoadApprovedFilmsAsync()
    {
        var approved = new Dictionary<int, ApprovedFilm>();
        if (!File.Exists(FilmsPath)) return approved;

        var lines = await File.ReadAllLinesAsync(FilmsPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var fields = ParseCsvLine(lines[i]);
            if (fields.Count >= 6 && int.TryParse(fields[0], out int id))
            {
                approved[id] = new ApprovedFilm(
                    id,
                    fields[1],
                    string.IsNullOrEmpty(fields[2]) ? null : fields[2],
                    fields[3],
                    string.IsNullOrEmpty(fields[4]) ? null : fields[4],
                    string.IsNullOrEmpty(fields[5]) ? null : fields[5]
                );
            }
        }
        return approved;
    }

    public async Task<HashSet<int>> LoadHeartsAsync()
    {
        var hearts = new HashSet<int>();
        if (!File.Exists(HeartsPath)) return hearts;

        var lines = await File.ReadAllLinesAsync(HeartsPath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            if (int.TryParse(lines[i].Trim(), out int tmdbId))
            {
                hearts.Add(tmdbId);
            }
        }
        return hearts;
    }

    public async Task WriteHeartsAsync(HashSet<int> hearts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("tmdbId");

        foreach (var id in hearts.OrderBy(id => id))
        {
            sb.AppendLine(id.ToString());
        }

        await File.WriteAllTextAsync(HeartsPath, sb.ToString());
    }

    public async Task WriteLogAsync(List<Film> films)
    {
        var sb = new StringBuilder();
        sb.AppendLine("date,title,location,companions,tmdbId");

        foreach (var film in films)
        {
            var companions = film.Companions.Contains(',')
                ? $"\"{film.Companions}\""
                : film.Companions;
            var title = film.Title.Contains(',')
                ? $"\"{film.Title}\""
                : film.Title;

            sb.AppendLine($"{film.Date},{title},{film.Location},{companions},{film.TmdbId?.ToString() ?? ""}");
        }

        await File.WriteAllTextAsync(LogPath, sb.ToString());
    }

    public async Task WriteApprovedFilmsAsync(Dictionary<int, ApprovedFilm> approved)
    {
        var sb = new StringBuilder();
        sb.AppendLine("tmdbId,title,director,releaseYear,language,posterPath");

        foreach (var film in approved.Values.OrderBy(f => f.TmdbId))
        {
            var title = film.Title.Contains(',') ? $"\"{film.Title}\"" : film.Title;
            var director = film.Director?.Contains(',') == true ? $"\"{film.Director}\"" : film.Director ?? "";

            sb.AppendLine($"{film.TmdbId},{title},{director},{film.ReleaseYear ?? ""},{film.Language ?? ""},{film.PosterPath ?? ""}");
        }

        await File.WriteAllTextAsync(FilmsPath, sb.ToString());
    }

    public List<string> GetRecentLocations(List<Film> films, int count = 10)
    {
        return films
            .Select(f => f.Location)
            .Where(l => !string.IsNullOrEmpty(l))
            .GroupBy(l => l)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToList();
    }

    private static List<Film> ParseLogCsv(string[] lines)
    {
        var films = new List<Film>();
        if (lines.Length == 0) return films;

        var header = ParseCsvLine(lines[0]);
        var tmdbIdIndex = header.IndexOf("tmdbId");

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var fields = ParseCsvLine(lines[i]);
            if (fields.Count >= 4)
            {
                int? tmdbId = null;
                if (tmdbIdIndex >= 0 && fields.Count > tmdbIdIndex && !string.IsNullOrEmpty(fields[tmdbIdIndex]))
                {
                    if (int.TryParse(fields[tmdbIdIndex], out int id))
                        tmdbId = id;
                }

                films.Add(new Film(
                    fields[0],
                    fields[1],
                    fields[2],
                    fields[3],
                    tmdbId
                ));
            }
        }
        return films;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
                current.Append(c);
        }
        result.Add(current.ToString().Trim());
        return result;
    }
}
