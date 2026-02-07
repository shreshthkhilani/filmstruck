using FilmStruck.Cli;
using FilmStruck.Cli.Services;
using NUnit.Framework;

namespace FilmStruck.Cli.Tests.Services;

[TestFixture]
public class StatsServiceTests
{
    private StatsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new StatsService();
    }

    [Test]
    public void CalculateStats_EmptyLog_ReturnsEmptyStats()
    {
        var log = new List<Film>();
        var films = new Dictionary<int, ApprovedFilm>();

        var stats = _service.CalculateStats(log, films);

        Assert.That(stats["watch_year"], Is.Empty);
        Assert.That(stats["director"], Is.Empty);
        Assert.That(stats["companion"], Is.Empty);
        Assert.That(stats["location"], Is.Empty);
        Assert.That(stats["release_decade"], Is.Empty);
    }

    [TestCase("1/15/2024", 2024)]
    [TestCase("12/31/2023", 2023)]
    [TestCase("6/1/2020", 2020)]
    public void CalculateStats_ParsesWatchYear(string date, int expectedYear)
    {
        var log = new List<Film> { new Film(date, "Test Film", "Home", "", 123) };
        var films = new Dictionary<int, ApprovedFilm>();

        var stats = _service.CalculateStats(log, films);

        Assert.That(stats["watch_year"].ContainsKey(expectedYear.ToString()), Is.True);
        Assert.That(stats["watch_year"][expectedYear.ToString()], Is.EqualTo(1));
    }

    [TestCase("Alice,Bob", new[] { "Alice", "Bob" })]
    [TestCase("Alice", new[] { "Alice" })]
    [TestCase("", new string[0])]
    [TestCase("  Alice  ,  Bob  ", new[] { "Alice", "Bob" })]
    public void CalculateStats_SplitsCompanions(string companions, string[] expected)
    {
        var log = new List<Film> { new Film("1/1/2024", "Test Film", "Home", companions, null) };
        var films = new Dictionary<int, ApprovedFilm>();

        var stats = _service.CalculateStats(log, films);

        foreach (var companion in expected)
        {
            Assert.That(stats["companion"].ContainsKey(companion), Is.True);
            Assert.That(stats["companion"][companion], Is.EqualTo(1));
        }
        Assert.That(stats["companion"].Count, Is.EqualTo(expected.Length));
    }

    [TestCase(1972, "1970s")]
    [TestCase(2024, "2020s")]
    [TestCase(1999, "1990s")]
    [TestCase(2000, "2000s")]
    public void CalculateStats_CalculatesDecade(int releaseYear, string expectedDecade)
    {
        var log = new List<Film> { new Film("1/1/2024", "Test Film", "Home", "", 123) };
        var films = new Dictionary<int, ApprovedFilm>
        {
            [123] = new ApprovedFilm(123, "Test Film", "Director", releaseYear.ToString(), "en", "/poster.jpg")
        };

        var stats = _service.CalculateStats(log, films);

        Assert.That(stats["release_decade"].ContainsKey(expectedDecade), Is.True);
        Assert.That(stats["release_decade"][expectedDecade], Is.EqualTo(1));
    }

    [Test]
    public void CalculateStats_CountsDirectors()
    {
        var log = new List<Film>
        {
            new Film("1/1/2024", "Film 1", "Home", "", 1),
            new Film("1/2/2024", "Film 2", "Home", "", 2),
            new Film("1/3/2024", "Film 3", "Home", "", 3)
        };
        var films = new Dictionary<int, ApprovedFilm>
        {
            [1] = new ApprovedFilm(1, "Film 1", "Nolan", "2020", "en", null),
            [2] = new ApprovedFilm(2, "Film 2", "Nolan", "2021", "en", null),
            [3] = new ApprovedFilm(3, "Film 3", "Spielberg", "2022", "en", null)
        };

        var stats = _service.CalculateStats(log, films);

        Assert.That(stats["director"]["Nolan"], Is.EqualTo(2));
        Assert.That(stats["director"]["Spielberg"], Is.EqualTo(1));
    }

    [Test]
    public void CalculateStats_CountsLocations()
    {
        var log = new List<Film>
        {
            new Film("1/1/2024", "Film 1", "Home", "", null),
            new Film("1/2/2024", "Film 2", "Home", "", null),
            new Film("1/3/2024", "Film 3", "Theater", "", null)
        };
        var films = new Dictionary<int, ApprovedFilm>();

        var stats = _service.CalculateStats(log, films);

        Assert.That(stats["location"]["Home"], Is.EqualTo(2));
        Assert.That(stats["location"]["Theater"], Is.EqualTo(1));
    }

    [Test]
    public void CalculateStats_CountsAllTime()
    {
        var log = new List<Film>
        {
            new Film("1/1/2023", "Film 1", "Home", "", null),
            new Film("1/1/2024", "Film 2", "Home", "", null),
            new Film("6/1/2024", "Film 3", "Home", "", null)
        };
        var films = new Dictionary<int, ApprovedFilm>();

        var stats = _service.CalculateStats(log, films);

        Assert.That(stats["watch_year"]["ALL_TIME"], Is.EqualTo(3));
        Assert.That(stats["watch_year"]["2024"], Is.EqualTo(2));
        Assert.That(stats["watch_year"]["2023"], Is.EqualTo(1));
    }
}
