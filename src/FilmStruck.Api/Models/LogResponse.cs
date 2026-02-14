using System.Text.Json.Serialization;

namespace FilmStruck.Api.Models;

public class LogResponse
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("entries")]
    public List<LogEntry> Entries { get; set; } = [];
}

public class LogEntry
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("companions")]
    public string Companions { get; set; } = string.Empty;

    [JsonPropertyName("tmdbId")]
    public string TmdbId { get; set; } = string.Empty;

    [JsonPropertyName("director")]
    public string? Director { get; set; }

    [JsonPropertyName("releaseYear")]
    public string? ReleaseYear { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }
}
