using Amazon.DynamoDBv2;
using FilmStruck.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var endpointUrl = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL");
    if (!string.IsNullOrEmpty(endpointUrl))
    {
        var config = new AmazonDynamoDBConfig { ServiceURL = endpointUrl };
        return new AmazonDynamoDBClient(config);
    }
    return new AmazonDynamoDBClient();
});

builder.Services.AddSingleton<WatchLogService>();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.MapGet("/api/{username}", async (string username, WatchLogService service) =>
{
    var response = await service.GetWatchLog(username);
    return Results.Ok(response);
});

app.Run();
