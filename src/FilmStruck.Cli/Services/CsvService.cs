using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FilmStruck.Cli.Services;

public class CsvService
{
    private const string LegacyFilmsHeader = "tmdbId,title,director,releaseYear,language,posterPath";
    private const string RuntimeColumn = "runtimeMinutes";
    internal const string FilmsHeader = LegacyFilmsHeader + "," + RuntimeColumn;

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
        if (lines.Length == 0) return approved;

        var runtimeIndex = ParseCsvLine(lines[0]).IndexOf(RuntimeColumn);
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
                )
                {
                    RuntimeMinutes = ReadRuntime(fields, runtimeIndex)
                };
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
        sb.AppendLine(FilmsHeader);

        foreach (var film in approved.Values.OrderBy(f => f.TmdbId))
        {
            var title = film.Title.Contains(',') ? $"\"{film.Title}\"" : film.Title;
            var director = film.Director?.Contains(',') == true ? $"\"{film.Director}\"" : film.Director ?? "";

            var runtime = film.RuntimeMinutes is > 0
                ? film.RuntimeMinutes.Value.ToString(CultureInfo.InvariantCulture)
                : "";
            sb.AppendLine($"{film.TmdbId},{title},{director},{film.ReleaseYear ?? ""},{film.Language ?? ""},{film.PosterPath ?? ""},{runtime}");
        }

        await File.WriteAllTextAsync(FilmsPath, sb.ToString());
    }

    internal async Task<int> BackfillRuntimesAsync(Func<int, Task<int?>> lookupRuntime)
    {
        if (!File.Exists(FilmsPath)) return 0;

        // Decode the BOM as part of the text and retain the original row separators.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var original = utf8.GetString(await File.ReadAllBytesAsync(FilmsPath));
        if (string.IsNullOrWhiteSpace(original)) return 0;

        var lines = Regex.Split(original, "(\r\n|\r|\n)");
        var header = SplitCsvLine(lines[0].TrimStart('\uFEFF'), out var headerHasOpenQuote)
            .Select(Unquote).ToList();
        var legacyColumns = LegacyFilmsHeader.Split(',');
        if (headerHasOpenQuote || !header.Take(legacyColumns.Length).SequenceEqual(legacyColumns) ||
            header.Count(column => column == RuntimeColumn) > 1)
        {
            throw new InvalidDataException("Unexpected films.csv header. Check the column names before backfilling.");
        }

        var runtimeIndex = header.IndexOf(RuntimeColumn);
        var appendColumn = runtimeIndex < 0;
        if (appendColumn)
            runtimeIndex = header.Count;

        var lookups = new Dictionary<int, int?>();
        var updated = 0;
        for (var i = 2; i < lines.Length; i += 2)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var fields = SplitCsvLine(lines[i], out var rowHasOpenQuote);
            if (rowHasOpenQuote)
                throw new InvalidDataException($"Unmatched quote on films.csv line {i / 2 + 1}.");
            if (fields.Count > header.Count || fields.Count < legacyColumns.Length)
            {
                throw new InvalidDataException($"Unexpected number of columns on films.csv line {i / 2 + 1}.");
            }

            if (appendColumn)
                lines[i] += new string(',', runtimeIndex + 1 - fields.Count);

            if (!int.TryParse(Unquote(fields[0]), out var id) || id <= 0 ||
                ReadRuntime(fields, runtimeIndex) is > 0)
                continue;

            if (!lookups.TryGetValue(id, out var runtime))
            {
                runtime = await lookupRuntime(id);
                lookups[id] = runtime;
            }

            if (runtime is not > 0) continue;

            while (fields.Count <= runtimeIndex)
                fields.Add("");

            fields[runtimeIndex] = runtime.Value.ToString(CultureInfo.InvariantCulture);
            lines[i] = string.Join(',', fields);
            updated++;
        }

        if (updated == 0) return 0;
        if (appendColumn)
            lines[0] += "," + RuntimeColumn;

        var tempPath = FilmsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, string.Concat(lines));
            if (utf8.GetString(await File.ReadAllBytesAsync(FilmsPath)) != original)
                throw new IOException("films.csv changed during backfill. Review those changes and retry.");

            File.Move(tempPath, FilmsPath, overwrite: true);
        }
        finally
        {
            File.Delete(tempPath);
        }

        return updated;
    }

    private static int? ReadRuntime(List<string> fields, int index)
    {
        return index >= 0 && index < fields.Count &&
            int.TryParse(Unquote(fields[index]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var runtime) &&
            runtime > 0
            ? runtime
            : null;
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

    private static List<string> ParseCsvLine(string line) =>
        SplitCsvLine(line, out _).Select(Unquote).ToList();

    private static string Unquote(string field) => field.Replace("\"", "").Trim();

    private static List<string> SplitCsvLine(string line, out bool inQuotes)
    {
        var result = new List<string>();
        var fieldStart = 0;
        inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
                inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes)
            {
                result.Add(line[fieldStart..i]);
                fieldStart = i + 1;
            }
        }
        result.Add(line[fieldStart..]);
        return result;
    }
}
