using Amazon.DynamoDBv2.DataModel;

namespace FilmStruck.Api.Models;

[DynamoDBTable("filmstruck")]
public class LogItem
{
    [DynamoDBHashKey("PartitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [DynamoDBRangeKey("SortKey")]
    public string SortKey { get; set; } = string.Empty;

    [DynamoDBProperty("date")]
    public string Date { get; set; } = string.Empty;

    [DynamoDBProperty("title")]
    public string Title { get; set; } = string.Empty;

    [DynamoDBProperty("location")]
    public string Location { get; set; } = string.Empty;

    [DynamoDBProperty("companions")]
    public string Companions { get; set; } = string.Empty;

    [DynamoDBProperty("tmdbId")]
    public string TmdbId { get; set; } = string.Empty;
}
