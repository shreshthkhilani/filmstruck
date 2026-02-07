using FilmStruck.Cli;
using FilmStruck.Cli.Services;
using NUnit.Framework;

namespace FilmStruck.Cli.Tests.Services;

[TestFixture]
public class CsvServiceTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"filmstruck-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "data"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private CsvService CreateService()
    {
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir);
            return new CsvService();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Test]
    public async Task LoadLogAsync_ParsesBasicCsv()
    {
        var logPath = Path.Combine(_tempDir, "data", "log.csv");
        await File.WriteAllTextAsync(logPath, @"date,title,location,companions,tmdbId
1/15/2024,Inception,Home,Alice,27205
1/16/2024,The Matrix,Theater,Bob,603");

        var service = CreateService();
        var films = await service.LoadLogAsync();

        Assert.That(films.Count, Is.EqualTo(2));
        Assert.That(films[0].Title, Is.EqualTo("Inception"));
        Assert.That(films[0].Date, Is.EqualTo("1/15/2024"));
        Assert.That(films[0].Location, Is.EqualTo("Home"));
        Assert.That(films[0].Companions, Is.EqualTo("Alice"));
        Assert.That(films[0].TmdbId, Is.EqualTo(27205));
        Assert.That(films[1].Title, Is.EqualTo("The Matrix"));
        Assert.That(films[1].TmdbId, Is.EqualTo(603));
    }

    [TestCase("\"Alice,Bob\"", "Alice,Bob")]
    [TestCase("\"Film with \"\"quotes\"\"\"", "Film with quotes")]
    public async Task LoadLogAsync_HandlesQuotedFields(string quotedValue, string expected)
    {
        var logPath = Path.Combine(_tempDir, "data", "log.csv");
        await File.WriteAllTextAsync(logPath, $@"date,title,location,companions,tmdbId
1/15/2024,Test Film,Home,{quotedValue},123");

        var service = CreateService();
        var films = await service.LoadLogAsync();

        Assert.That(films.Count, Is.EqualTo(1));
        // The quoted value is in companions field
        Assert.That(films[0].Companions, Is.EqualTo(expected));
    }

    [Test]
    public async Task LoadLogAsync_HandlesEmptyFile()
    {
        var logPath = Path.Combine(_tempDir, "data", "log.csv");
        await File.WriteAllTextAsync(logPath, "date,title,location,companions,tmdbId");

        var service = CreateService();
        var films = await service.LoadLogAsync();

        Assert.That(films, Is.Empty);
    }

    [Test]
    public async Task LoadApprovedFilmsAsync_ParsesAllFields()
    {
        var filmsPath = Path.Combine(_tempDir, "data", "films.csv");
        await File.WriteAllTextAsync(filmsPath, @"tmdbId,title,director,releaseYear,language,posterPath
27205,Inception,Christopher Nolan,2010,en,/poster1.jpg
603,The Matrix,Lana Wachowski,1999,en,/poster2.jpg");

        var service = CreateService();
        var films = await service.LoadApprovedFilmsAsync();

        Assert.That(films.Count, Is.EqualTo(2));
        Assert.That(films[27205].Title, Is.EqualTo("Inception"));
        Assert.That(films[27205].Director, Is.EqualTo("Christopher Nolan"));
        Assert.That(films[27205].ReleaseYear, Is.EqualTo("2010"));
        Assert.That(films[27205].Language, Is.EqualTo("en"));
        Assert.That(films[27205].PosterPath, Is.EqualTo("/poster1.jpg"));
    }

    [Test]
    public async Task LoadApprovedFilmsAsync_ReturnsEmptyWhenNoFile()
    {
        var service = CreateService();
        var films = await service.LoadApprovedFilmsAsync();

        Assert.That(films, Is.Empty);
    }

    [Test]
    public async Task LoadHeartsAsync_ParsesIds()
    {
        var heartsPath = Path.Combine(_tempDir, "data", "hearts.csv");
        await File.WriteAllTextAsync(heartsPath, @"tmdbId
27205
603
278");

        var service = CreateService();
        var hearts = await service.LoadHeartsAsync();

        Assert.That(hearts.Count, Is.EqualTo(3));
        Assert.That(hearts.Contains(27205), Is.True);
        Assert.That(hearts.Contains(603), Is.True);
        Assert.That(hearts.Contains(278), Is.True);
    }

    [Test]
    public async Task WriteLogAsync_QuotesFieldsWithCommas()
    {
        var service = CreateService();
        var films = new List<Film>
        {
            new Film("1/15/2024", "Test Film", "Home", "Alice,Bob", 123)
        };

        await service.WriteLogAsync(films);

        var content = await File.ReadAllTextAsync(service.LogPath);
        Assert.That(content, Does.Contain("\"Alice,Bob\""));
    }

    [Test]
    public void GetRecentLocations_OrdersByFrequency()
    {
        var films = new List<Film>
        {
            new Film("1/1/2024", "Film 1", "Home", "", null),
            new Film("1/2/2024", "Film 2", "Home", "", null),
            new Film("1/3/2024", "Film 3", "Home", "", null),
            new Film("1/4/2024", "Film 4", "Theater", "", null),
            new Film("1/5/2024", "Film 5", "Theater", "", null),
            new Film("1/6/2024", "Film 6", "Airplane", "", null)
        };

        var service = CreateService();
        var locations = service.GetRecentLocations(films);

        Assert.That(locations[0], Is.EqualTo("Home"));
        Assert.That(locations[1], Is.EqualTo("Theater"));
        Assert.That(locations[2], Is.EqualTo("Airplane"));
    }
}
