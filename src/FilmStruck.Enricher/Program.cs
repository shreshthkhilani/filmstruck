using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var TMDB_API_KEY = Environment.GetEnvironmentVariable("TMDB_API_KEY")
    ?? throw new Exception("TMDB_API_KEY environment variable is required");

var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
var logPath = Path.Combine(repoRoot, "data", "log.csv");
var filmsPath = Path.Combine(repoRoot, "data", "films.csv");

Console.WriteLine($"Reading from: {logPath}");

var lines = await File.ReadAllLinesAsync(logPath);
var films = ParseFilmsCsv(lines);

Console.WriteLine($"Found {films.Count} films total");

// Load existing approved films
var approvedFilms = await LoadApprovedFilms(filmsPath);
Console.WriteLine($"Loaded {approvedFilms.Count} approved films");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {TMDB_API_KEY}");
http.DefaultRequestHeaders.Add("Accept", "application/json");

// Sync films that have tmdbId but are missing from films.csv
var filmsWithIdButNotApproved = films
    .Where(f => f.TmdbId.HasValue && !approvedFilms.ContainsKey(f.TmdbId.Value))
    .ToList();

if (filmsWithIdButNotApproved.Count > 0)
{
    Console.WriteLine($"\nSyncing {filmsWithIdButNotApproved.Count} films to films.csv...");
    foreach (var film in filmsWithIdButNotApproved)
    {
        try
        {
            var movie = await GetMovieDetails(http, film.TmdbId!.Value);
            if (movie != null)
            {
                var director = await GetDirector(http, film.TmdbId.Value);
                var year = !string.IsNullOrEmpty(movie.ReleaseDate) && movie.ReleaseDate.Length >= 4
                    ? movie.ReleaseDate[..4]
                    : null;
                
                var approved = new ApprovedFilm(
                    film.TmdbId.Value,
                    movie.Title ?? film.Title,
                    director,
                    year,
                    movie.OriginalLanguage,
                    movie.PosterPath
                );
                approvedFilms[film.TmdbId.Value] = approved;
                Console.WriteLine($"  ✓ {approved.Title} ({approved.ReleaseYear ?? "?"}) - {approved.Director ?? "Unknown"}");
                await Task.Delay(50);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ {film.Title}: {ex.Message}");
        }
    }
    await WriteApprovedFilms(filmsPath, approvedFilms);
    Console.WriteLine("Sync complete.\n");
}

var filmsToProcess = films.Where(f => f.TmdbId == null).ToList();
Console.WriteLine($"\n{filmsToProcess.Count} films need TMDB lookup\n");

for (int i = 0; i < filmsToProcess.Count; i++)
{
    var film = filmsToProcess[i];
    Console.WriteLine($"[{i + 1}/{filmsToProcess.Count}] {film.Title}");

    try
    {
        var searchResults = await SearchMovies(http, film.Title);

        if (searchResults.Count == 0)
        {
            Console.WriteLine("  No results found. Press Enter to continue...");
            Console.ReadLine();
            continue;
        }

        // Fetch directors for top 5 results
        var options = new List<MovieOption>();
        foreach (var result in searchResults.Take(5))
        {
            var director = await GetDirector(http, result.Id);
            var year = !string.IsNullOrEmpty(result.ReleaseDate) && result.ReleaseDate.Length >= 4
                ? result.ReleaseDate[..4]
                : "????";
            options.Add(new MovieOption(result, director, year));
            await Task.Delay(50); // Rate limit
        }

        // Display options
        Console.WriteLine();
        for (int j = 0; j < options.Count; j++)
        {
            var opt = options[j];
            Console.WriteLine($"  [{j + 1}] {opt.Movie.Title} ({opt.Year}) - {opt.Director ?? "Unknown director"}");
        }
        Console.WriteLine("  [i] Enter TMDB ID  [s] Skip  [q] Quit");
        Console.Write("\n  Select: ");

        var input = Console.ReadLine()?.Trim().ToLower();

        if (input == "q")
        {
            Console.WriteLine("\nQuitting...");
            break;
        }

        if (input == "s" || string.IsNullOrEmpty(input))
        {
            Console.WriteLine("  Skipped\n");
            continue;
        }

        if (input == "i")
        {
            Console.Write("  Enter TMDB ID: ");
            var idInput = Console.ReadLine()?.Trim();
            if (int.TryParse(idInput, out int manualId))
            {
                try
                {
                    var movie = await GetMovieDetails(http, manualId);
                    if (movie == null)
                    {
                        Console.WriteLine("  Movie not found\n");
                        continue;
                    }
                    
                    var director = await GetDirector(http, manualId);
                    var year = !string.IsNullOrEmpty(movie.ReleaseDate) && movie.ReleaseDate.Length >= 4
                        ? movie.ReleaseDate[..4]
                        : "????";
                    
                    Console.WriteLine($"\n  Found: {movie.Title} ({year}) - {director ?? "Unknown director"}");
                    Console.Write("  Confirm? [y/n]: ");
                    var confirm = Console.ReadLine()?.Trim().ToLower();
                    
                    if (confirm == "y")
                    {
                        film.TmdbId = manualId;
                        
                        var approved = new ApprovedFilm(
                            manualId,
                            movie.Title ?? film.Title,
                            director,
                            year,
                            movie.OriginalLanguage,
                            movie.PosterPath
                        );
                        approvedFilms[manualId] = approved;
                        
                        await WriteLogCsv(logPath, films);
                        await WriteApprovedFilms(filmsPath, approvedFilms);
                        
                        Console.WriteLine($"  ✓ Saved: {approved.Title} ({approved.ReleaseYear}) - {approved.Director}\n");
                    }
                    else
                    {
                        Console.WriteLine("  Cancelled\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error fetching movie: {ex.Message}\n");
                }
            }
            else
            {
                Console.WriteLine("  Invalid ID\n");
            }
            continue;
        }

        if (int.TryParse(input, out int selection) && selection >= 1 && selection <= options.Count)
        {
            var selected = options[selection - 1];
            
            // Update film with tmdbId
            film.TmdbId = selected.Movie.Id;

            // Add to approved films
            var approved = new ApprovedFilm(
                selected.Movie.Id,
                selected.Movie.Title ?? film.Title,
                selected.Director,
                selected.Year,
                selected.Movie.OriginalLanguage,
                selected.Movie.PosterPath
            );
            approvedFilms[selected.Movie.Id] = approved;

            // Save immediately
            await WriteLogCsv(logPath, films);
            await WriteApprovedFilms(filmsPath, approvedFilms);

            Console.WriteLine($"  ✓ Saved: {approved.Title} ({approved.ReleaseYear}) - {approved.Director}\n");
        }
        else
        {
            Console.WriteLine("  Invalid selection, skipped\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}\n");
    }
}

Console.WriteLine("\nDone!");

// --- Helper methods ---

static string FindRepoRoot(string startDir)
{
    var dir = startDir;
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir, ".git")))
            return dir;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new Exception("Could not find repository root");
}

static List<Film> ParseFilmsCsv(string[] lines)
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

static List<string> ParseCsvLine(string line)
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

static async Task<Dictionary<int, ApprovedFilm>> LoadApprovedFilms(string path)
{
    var approved = new Dictionary<int, ApprovedFilm>();
    if (!File.Exists(path)) return approved;

    var lines = await File.ReadAllLinesAsync(path);
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

static async Task<List<TmdbMovie>> SearchMovies(HttpClient http, string title)
{
    var encoded = Uri.EscapeDataString(title);
    var url = $"https://api.themoviedb.org/3/search/movie?query={encoded}&include_adult=false&language=en-US&page=1";

    var response = await http.GetFromJsonAsync<TmdbSearchResponse>(url);
    return response?.Results ?? new List<TmdbMovie>();
}

static async Task<TmdbMovie?> GetMovieDetails(HttpClient http, int movieId)
{
    var url = $"https://api.themoviedb.org/3/movie/{movieId}";
    return await http.GetFromJsonAsync<TmdbMovie>(url);
}

static async Task<string?> GetDirector(HttpClient http, int movieId)
{
    var url = $"https://api.themoviedb.org/3/movie/{movieId}/credits";
    var response = await http.GetFromJsonAsync<TmdbCreditsResponse>(url);
    var directors = response?.Crew?
        .Where(c => c.Job == "Director")
        .Select(c => c.Name)
        .Where(n => n != null)
        .ToList();
    
    return directors?.Count > 0 ? string.Join(",", directors!) : null;
}

static async Task WriteLogCsv(string path, List<Film> films)
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

    await File.WriteAllTextAsync(path, sb.ToString());
}

static async Task WriteApprovedFilms(string path, Dictionary<int, ApprovedFilm> approved)
{
    var sb = new StringBuilder();
    sb.AppendLine("tmdbId,title,director,releaseYear,language,posterPath");

    foreach (var film in approved.Values.OrderBy(f => f.TmdbId))
    {
        var title = film.Title.Contains(',') ? $"\"{film.Title}\"" : film.Title;
        var director = film.Director?.Contains(',') == true ? $"\"{film.Director}\"" : film.Director ?? "";

        sb.AppendLine($"{film.TmdbId},{title},{director},{film.ReleaseYear ?? ""},{film.Language ?? ""},{film.PosterPath ?? ""}");
    }

    await File.WriteAllTextAsync(path, sb.ToString());
}

// --- Types ---

class Film
{
    public string Date { get; }
    public string Title { get; }
    public string Location { get; }
    public string Companions { get; }
    public int? TmdbId { get; set; }

    public Film(string date, string title, string location, string companions, int? tmdbId)
    {
        Date = date;
        Title = title;
        Location = location;
        Companions = companions;
        TmdbId = tmdbId;
    }
}

record ApprovedFilm(
    int TmdbId,
    string Title,
    string? Director,
    string? ReleaseYear,
    string? Language,
    string? PosterPath
);

record MovieOption(TmdbMovie Movie, string? Director, string Year);

record TmdbSearchResponse(
    [property: JsonPropertyName("results")] List<TmdbMovie>? Results
);

record TmdbMovie(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("original_language")] string? OriginalLanguage,
    [property: JsonPropertyName("poster_path")] string? PosterPath
);

record TmdbCreditsResponse(
    [property: JsonPropertyName("crew")] List<TmdbCrewMember>? Crew
);

record TmdbCrewMember(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("job")] string? Job
);
