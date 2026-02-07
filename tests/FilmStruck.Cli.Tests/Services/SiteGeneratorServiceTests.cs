using FilmStruck.Cli;
using FilmStruck.Cli.Services;
using NUnit.Framework;

namespace FilmStruck.Cli.Tests.Services;

[TestFixture]
public class SiteGeneratorServiceTests
{
    private SiteGeneratorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new SiteGeneratorService();
    }

    [Test]
    public void GenerateHtml_ReplacesUsernameToken()
    {
        var films = new List<WatchedFilm>();
        var companions = new List<CompanionCount>();
        var hearts = new HashSet<int>();
        var config = new FilmStruckConfig { Username = "testuser", SiteTitle = "filmstruck" };

        var html = _service.GenerateHtml(films, companions, hearts, config);

        Assert.That(html, Does.Not.Contain("{{USERNAME}}"));
        Assert.That(html, Does.Contain("testuser"));
    }

    [Test]
    public void GenerateHtml_EmbedsFilmsAsJson()
    {
        var films = new List<WatchedFilm>
        {
            new WatchedFilm(
                "1/15/2024",
                "Home",
                "Alice",
                "27205",
                "Inception",
                "2010",
                "Christopher Nolan",
                "en",
                "/poster.jpg"
            )
        };
        var companions = new List<CompanionCount>();
        var hearts = new HashSet<int>();
        var config = new FilmStruckConfig { Username = "testuser" };

        var html = _service.GenerateHtml(films, companions, hearts, config);

        Assert.That(html, Does.Not.Contain("{{FILMS_DATA}}"));
        Assert.That(html, Does.Contain("Inception"));
        Assert.That(html, Does.Contain("27205"));
        Assert.That(html, Does.Contain("Christopher Nolan"));
    }

    [Test]
    public void GenerateHtml_EmbedsHeartsAsJson()
    {
        var films = new List<WatchedFilm>();
        var companions = new List<CompanionCount>();
        var hearts = new HashSet<int> { 27205, 603, 278 };
        var config = new FilmStruckConfig { Username = "testuser" };

        var html = _service.GenerateHtml(films, companions, hearts, config);

        Assert.That(html, Does.Not.Contain("{{HEARTS_DATA}}"));
        Assert.That(html, Does.Contain("27205"));
        Assert.That(html, Does.Contain("603"));
        Assert.That(html, Does.Contain("278"));
    }
}
