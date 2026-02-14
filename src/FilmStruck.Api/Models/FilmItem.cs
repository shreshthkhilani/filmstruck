using Amazon.DynamoDBv2.DataModel;

namespace FilmStruck.Api.Models;

[DynamoDBTable("filmstruck")]
public class FilmItem
{
    [DynamoDBHashKey("PartitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [DynamoDBRangeKey("SortKey")]
    public string SortKey { get; set; } = string.Empty;

    [DynamoDBProperty("tmdbId")]
    public string TmdbId { get; set; } = string.Empty;

    [DynamoDBProperty("title")]
    public string Title { get; set; } = string.Empty;

    [DynamoDBProperty("director")]
    public string Director { get; set; } = string.Empty;

    [DynamoDBProperty("releaseYear")]
    public string ReleaseYear { get; set; } = string.Empty;

    [DynamoDBProperty("language")]
    public string Language { get; set; } = string.Empty;

    [DynamoDBProperty("posterPath")]
    public string PosterPath { get; set; } = string.Empty;
}
