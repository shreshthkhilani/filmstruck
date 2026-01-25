using System.Text.Json.Serialization;

namespace FilmStruck.Cli;

public class Film
{
    public string Date { get; set; }
    public string Title { get; set; }
    public string Location { get; set; }
    public string Companions { get; set; }
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

public record ApprovedFilm(
    int TmdbId,
    string Title,
    string? Director,
    string? ReleaseYear,
    string? Language,
    string? PosterPath
);

public record MovieOption(TmdbMovie Movie, string? Director, string Year);

public record TmdbSearchResponse(
    [property: JsonPropertyName("results")] List<TmdbMovie>? Results
);

public record TmdbMovie(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("original_language")] string? OriginalLanguage,
    [property: JsonPropertyName("poster_path")] string? PosterPath
);

public record TmdbCreditsResponse(
    [property: JsonPropertyName("crew")] List<TmdbCrewMember>? Crew
);

public record TmdbCrewMember(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("job")] string? Job
);

public record TmdbImagesResponse(
    [property: JsonPropertyName("posters")] List<TmdbPoster>? Posters
);

public record TmdbPoster(
    [property: JsonPropertyName("file_path")] string? FilePath,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("iso_639_1")] string? Language,
    [property: JsonPropertyName("vote_average")] double VoteAverage,
    [property: JsonPropertyName("vote_count")] int VoteCount,
    [property: JsonPropertyName("contributor")] string? Contributor = null
);

// View models for site generation
public record WatchedFilm(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("location")] string Location,
    [property: JsonPropertyName("companions")] string Companions,
    [property: JsonPropertyName("tmdbId")] string TmdbId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("releaseYear")] string ReleaseYear,
    [property: JsonPropertyName("director")] string Director,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("posterPath")] string PosterPath
);

public record CompanionCount(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("count")] int Count
);
