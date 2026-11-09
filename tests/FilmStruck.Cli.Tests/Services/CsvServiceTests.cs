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
    public async Task LoadLogAsync_ParsesBlankTmdbId()
    {
        var logPath = Path.Combine(_tempDir, "data", "log.csv");
        await File.WriteAllTextAsync(logPath, @"date,title,location,companions,tmdbId
9/6/2026,Festival Short,Arverne Cinema,""Jack,Eli"",");

        var service = CreateService();
        var films = await service.LoadLogAsync();

        Assert.That(films, Has.Count.EqualTo(1));
        Assert.That(films[0].TmdbId, Is.Null);
        Assert.That(films[0].Title, Is.EqualTo("Festival Short"));
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
        Assert.That(films[27205].RuntimeMinutes, Is.Null);
    }

    [TestCase("120", 120)]
    [TestCase("", null)]
    [TestCase("0", null)]
    [TestCase("-20", null)]
    [TestCase("unknown", null)]
    public async Task LoadApprovedFilmsAsync_ReadsOptionalPositiveRuntime(string value, int? expected)
    {
        var service = CreateService();
        await File.WriteAllTextAsync(service.FilmsPath,
            $"tmdbId,title,director,releaseYear,language,posterPath,notes,runtimeMinutes\n603,Matrix,,1999,en,,note,{value}");

        var films = await service.LoadApprovedFilmsAsync();

        Assert.That(films[603].RuntimeMinutes, Is.EqualTo(expected));
    }

    [Test]
    public async Task WriteApprovedFilmsAsync_RoundTripsRuntimeAndUnknown()
    {
        var service = CreateService();
        var films = new Dictionary<int, ApprovedFilm>
        {
            [603] = new(603, "The Matrix", null, "1999", "en", null) { RuntimeMinutes = 136 },
            [238] = new(238, "The Godfather", null, "1972", "en", null)
        };

        await service.WriteApprovedFilmsAsync(films);

        var reloaded = await service.LoadApprovedFilmsAsync();
        Assert.That(reloaded[603].RuntimeMinutes, Is.EqualTo(136));
        Assert.That(reloaded[238].RuntimeMinutes, Is.Null);
    }

    [Test]
    public async Task BackfillRuntimesAsync_AppendsColumnWithoutChangingOtherFields()
    {
        var service = CreateService();
        const string header = "tmdbId,title,director,releaseYear,language,posterPath,notes";
        const string customRow = "603,\"Custom, \"\"title\"\"\",Director,1999,en,/custom.jpg, keep spaces ";
        const string missingId = ",Unlinked,,,,,keep";
        const string unknown = "238,The Godfather,,1972,en,,keep";
        await File.WriteAllTextAsync(service.FilmsPath, $"\uFEFF{header}\r\n{customRow}\r\n{missingId}\r\n{unknown}");

        var requests = new List<int>();
        Task<int?> Lookup(int id)
        {
            requests.Add(id);
            return Task.FromResult<int?>(id == 603 ? 136 : null);
        }

        Assert.That(await service.BackfillRuntimesAsync(Lookup), Is.EqualTo(1));
        var expected = $"\uFEFF{header},runtimeMinutes\r\n{customRow},136\r\n{missingId},\r\n{unknown},";
        var actual = System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(service.FilmsPath));
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(requests, Is.EqualTo(new[] { 603, 238 }));

        requests.Clear();
        Assert.That(await service.BackfillRuntimesAsync(Lookup), Is.Zero);
        Assert.That(requests, Is.EqualTo(new[] { 238 }));
        Assert.That(System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(service.FilmsPath)),
            Is.EqualTo(expected));
    }

    [Test]
    public async Task BackfillRuntimesAsync_UpdatesOnlyRuntimeColumn()
    {
        var service = CreateService();
        const string header = "tmdbId,title,director,releaseYear,language,posterPath,runtimeMinutes,notes";
        await File.WriteAllTextAsync(service.FilmsPath,
            header + "\n603,The Matrix,,1999,en,,0,keep\n238,The Godfather,,1972,en,,175,keep\n");

        var updated = await service.BackfillRuntimesAsync(id =>
        {
            Assert.That(id, Is.EqualTo(603));
            return Task.FromResult<int?>(136);
        });

        Assert.That(updated, Is.EqualTo(1));
        Assert.That(await File.ReadAllTextAsync(service.FilmsPath),
            Is.EqualTo(header + "\n603,The Matrix,,1999,en,,136,keep\n238,The Godfather,,1972,en,,175,keep\n"));
    }

    [Test]
    public async Task BackfillRuntimesAsync_LeavesOriginalWhenLookupFails()
    {
        var service = CreateService();
        const string original = "tmdbId,title,director,releaseYear,language,posterPath\n" +
            "603,The Matrix,,1999,en,\n238,The Godfather,,1972,en,\n";
        await File.WriteAllTextAsync(service.FilmsPath, original);

        Assert.ThrowsAsync<HttpRequestException>(async () => await service.BackfillRuntimesAsync(
            id => id == 603 ? Task.FromResult<int?>(136) : throw new HttpRequestException("Unavailable")));
        Assert.That(await File.ReadAllTextAsync(service.FilmsPath), Is.EqualTo(original));
    }

    [Test]
    public async Task BackfillRuntimesAsync_RefusesToAppendInsideAnUnclosedField()
    {
        var service = CreateService();
        const string original = "tmdbId,title,director,releaseYear,language,posterPath\n" +
            "603,The Matrix,,1999,en,\n238,The Godfather,,1972,en,\"/custom.jpg\n";
        await File.WriteAllTextAsync(service.FilmsPath, original);

        Assert.ThrowsAsync<InvalidDataException>(async () =>
            await service.BackfillRuntimesAsync(_ => Task.FromResult<int?>(136)));
        Assert.That(await File.ReadAllTextAsync(service.FilmsPath), Is.EqualTo(original));
    }

    [Test]
    public async Task BackfillRuntimesAsync_RefusesToOverwriteConcurrentChanges()
    {
        var service = CreateService();
        const string original = "tmdbId,title,director,releaseYear,language,posterPath\n603,The Matrix,,1999,en,\n";
        var edited = original.Replace("The Matrix", "Edited title");
        await File.WriteAllTextAsync(service.FilmsPath, original);

        Assert.ThrowsAsync<IOException>(async () => await service.BackfillRuntimesAsync(async _ =>
        {
            await File.WriteAllTextAsync(service.FilmsPath, edited);
            return 136;
        }));
        Assert.That(await File.ReadAllTextAsync(service.FilmsPath), Is.EqualTo(edited));
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
