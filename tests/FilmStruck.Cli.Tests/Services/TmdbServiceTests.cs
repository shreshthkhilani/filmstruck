using FilmStruck.Cli.Services;
using FilmStruck.Cli.Tests.Helpers;
using NUnit.Framework;

namespace FilmStruck.Cli.Tests.Services;

[TestFixture]
public class TmdbServiceTests
{
    private MockHttpHandler _mockHandler = null!;
    private TmdbService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHandler = new MockHttpHandler();
        var httpClient = new HttpClient(_mockHandler);
        _service = new TmdbService(httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task SearchMoviesAsync_ReturnsResults()
    {
        _mockHandler.SetResponse("search/movie", @"{
            ""results"": [
                { ""id"": 27205, ""title"": ""Inception"", ""release_date"": ""2010-07-16"", ""original_language"": ""en"" },
                { ""id"": 603, ""title"": ""The Matrix"", ""release_date"": ""1999-03-30"", ""original_language"": ""en"" }
            ]
        }");

        var results = await _service.SearchMoviesAsync("inception");

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].Id, Is.EqualTo(27205));
        Assert.That(results[0].Title, Is.EqualTo("Inception"));
        Assert.That(results[1].Id, Is.EqualTo(603));
    }

    [Test]
    public async Task SearchMoviesAsync_ReturnsEmptyOnNoResults()
    {
        _mockHandler.SetResponse("search/movie", @"{ ""results"": [] }");

        var results = await _service.SearchMoviesAsync("nonexistentmovie12345");

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task GetDirectorAsync_ExtractsDirector()
    {
        _mockHandler.SetResponse("credits", @"{
            ""crew"": [
                { ""name"": ""Christopher Nolan"", ""job"": ""Director"" },
                { ""name"": ""Hans Zimmer"", ""job"": ""Composer"" }
            ]
        }");

        var director = await _service.GetDirectorAsync(27205);

        Assert.That(director, Is.EqualTo("Christopher Nolan"));
    }

    [Test]
    public async Task GetDirectorAsync_JoinsMultipleDirectors()
    {
        _mockHandler.SetResponse("credits", @"{
            ""crew"": [
                { ""name"": ""Lana Wachowski"", ""job"": ""Director"" },
                { ""name"": ""Lilly Wachowski"", ""job"": ""Director"" }
            ]
        }");

        var director = await _service.GetDirectorAsync(603);

        Assert.That(director, Is.EqualTo("Lana Wachowski,Lilly Wachowski"));
    }

    [Test]
    public async Task GetMoviePostersAsync_OrdersByVoteAverage()
    {
        _mockHandler.SetResponse("images", @"{
            ""posters"": [
                { ""file_path"": ""/poster1.jpg"", ""vote_average"": 5.0, ""vote_count"": 10, ""width"": 500, ""height"": 750 },
                { ""file_path"": ""/poster2.jpg"", ""vote_average"": 8.5, ""vote_count"": 20, ""width"": 500, ""height"": 750 },
                { ""file_path"": ""/poster3.jpg"", ""vote_average"": 7.0, ""vote_count"": 15, ""width"": 500, ""height"": 750 }
            ]
        }");

        var posters = await _service.GetMoviePostersAsync(27205);

        Assert.That(posters.Count, Is.EqualTo(3));
        Assert.That(posters[0].FilePath, Is.EqualTo("/poster2.jpg"));
        Assert.That(posters[1].FilePath, Is.EqualTo("/poster3.jpg"));
        Assert.That(posters[2].FilePath, Is.EqualTo("/poster1.jpg"));
    }
}
