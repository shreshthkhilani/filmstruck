using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FilmStruck.Api.Models;

namespace FilmStruck.Api.Services;

public class WatchLogService
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public WatchLogService(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
        _tableName = Environment.GetEnvironmentVariable("TABLE_NAME") ?? "filmstruck-staging";
    }

    public async Task<LogResponse> GetWatchLog(string username)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PartitionKey = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = username } }
            }
        };

        var result = await _dynamoDb.QueryAsync(request);

        var logItems = new List<LogItem>();
        var filmItems = new Dictionary<string, FilmItem>();

        foreach (var item in result.Items)
        {
            var sortKey = item["SortKey"].S;

            if (sortKey.StartsWith("Log#"))
            {
                logItems.Add(new LogItem
                {
                    PartitionKey = item["PartitionKey"].S,
                    SortKey = sortKey,
                    Date = GetStringValue(item, "date"),
                    Title = GetStringValue(item, "title"),
                    Location = GetStringValue(item, "location"),
                    Companions = GetStringValue(item, "companions"),
                    TmdbId = GetStringValue(item, "tmdbId"),
                });
            }
            else if (sortKey.StartsWith("Film#"))
            {
                var tmdbId = GetStringValue(item, "tmdbId");
                filmItems[tmdbId] = new FilmItem
                {
                    PartitionKey = item["PartitionKey"].S,
                    SortKey = sortKey,
                    TmdbId = tmdbId,
                    Title = GetStringValue(item, "title"),
                    Director = GetStringValue(item, "director"),
                    ReleaseYear = GetStringValue(item, "releaseYear"),
                    Language = GetStringValue(item, "language"),
                    PosterPath = GetStringValue(item, "posterPath"),
                };
            }
        }

        var entries = logItems
            .Select(log =>
            {
                var entry = new LogEntry
                {
                    Date = log.Date,
                    Title = log.Title,
                    Location = log.Location,
                    Companions = log.Companions,
                    TmdbId = log.TmdbId,
                };

                if (filmItems.TryGetValue(log.TmdbId, out var film))
                {
                    entry.Director = film.Director;
                    entry.ReleaseYear = film.ReleaseYear;
                    entry.Language = film.Language;
                    entry.PosterPath = film.PosterPath;
                }

                return entry;
            })
            .OrderByDescending(e => ParseDate(e.Date))
            .ToList();

        return new LogResponse
        {
            Username = username,
            Count = entries.Count,
            Entries = entries,
        };
    }

    private static string GetStringValue(Dictionary<string, AttributeValue> item, string key)
    {
        return item.TryGetValue(key, out var value) ? value.S ?? string.Empty : string.Empty;
    }

    private static DateTime ParseDate(string date)
    {
        if (DateTime.TryParseExact(date, "M/d/yyyy", null, System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;
        if (DateTime.TryParse(date, out var fallback))
            return fallback;
        return DateTime.MinValue;
    }
}
