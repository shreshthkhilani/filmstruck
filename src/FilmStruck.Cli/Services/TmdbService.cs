using System.Net.Http.Json;

namespace FilmStruck.Cli.Services;

public class TmdbService : IDisposable
{
    private readonly HttpClient _http;

    public TmdbService(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    // Constructor for testing with injected HttpClient
    public TmdbService(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public async Task<List<TmdbMovie>> SearchMoviesAsync(string title)
    {
        var encoded = Uri.EscapeDataString(title);
        var url = $"https://api.themoviedb.org/3/search/movie?query={encoded}&include_adult=false&language=en-US&page=1";
        var response = await _http.GetFromJsonAsync<TmdbSearchResponse>(url);
        return response?.Results ?? new List<TmdbMovie>();
    }

    public async Task<TmdbMovie?> GetMovieDetailsAsync(int movieId)
    {
        var url = $"https://api.themoviedb.org/3/movie/{movieId}";
        return await _http.GetFromJsonAsync<TmdbMovie>(url);
    }

    public async Task<string?> GetDirectorAsync(int movieId)
    {
        var url = $"https://api.themoviedb.org/3/movie/{movieId}/credits";
        var response = await _http.GetFromJsonAsync<TmdbCreditsResponse>(url);
        var directors = response?.Crew?
            .Where(c => c.Job == "Director")
            .Select(c => c.Name)
            .Where(n => n != null)
            .ToList();

        return directors?.Count > 0 ? string.Join(",", directors!) : null;
    }

    public async Task<List<MovieOption>> GetMovieOptionsAsync(List<TmdbMovie> searchResults, int maxResults = 5)
    {
        var options = new List<MovieOption>();
        foreach (var result in searchResults.Take(maxResults))
        {
            var director = await GetDirectorAsync(result.Id);
            var year = !string.IsNullOrEmpty(result.ReleaseDate) && result.ReleaseDate.Length >= 4
                ? result.ReleaseDate[..4]
                : "????";
            options.Add(new MovieOption(result, director, year));
            await Task.Delay(50); // Rate limit
        }
        return options;
    }

    public async Task<List<TmdbPoster>> GetMoviePostersAsync(int movieId)
    {
        var url = $"https://api.themoviedb.org/3/movie/{movieId}/images";
        var response = await _http.GetFromJsonAsync<TmdbImagesResponse>(url);
        return response?.Posters?
            .OrderByDescending(p => p.VoteAverage)
            .ThenByDescending(p => p.VoteCount)
            .ToList() ?? new List<TmdbPoster>();
    }

    public async Task<(ApprovedFilm? Film, string? Error)> GetApprovedFilmAsync(int tmdbId, string fallbackTitle)
    {
        try
        {
            var movie = await GetMovieDetailsAsync(tmdbId);
            if (movie == null)
                return (null, "Movie not found");

            var director = await GetDirectorAsync(tmdbId);
            var year = !string.IsNullOrEmpty(movie.ReleaseDate) && movie.ReleaseDate.Length >= 4
                ? movie.ReleaseDate[..4]
                : null;

            var approved = new ApprovedFilm(
                tmdbId,
                movie.Title ?? fallbackTitle,
                director,
                year,
                movie.OriginalLanguage,
                movie.PosterPath
            );
            return (approved, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
