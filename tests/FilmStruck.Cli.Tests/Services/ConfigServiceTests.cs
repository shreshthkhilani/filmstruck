using FilmStruck.Cli.Services;
using NUnit.Framework;

namespace FilmStruck.Cli.Tests.Services;

[TestFixture]
public class ConfigServiceTests
{
    private string _tempDir = null!;
    private ConfigService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"filmstruck-config-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new ConfigService();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Test]
    public void LoadConfig_ReturnsDefaultWhenNoFile()
    {
        var config = _service.LoadConfig(_tempDir);

        Assert.That(config.Username, Is.EqualTo("user"));
        Assert.That(config.SiteTitle, Is.EqualTo("filmstruck"));
    }

    [Test]
    public void LoadConfig_ParsesJson()
    {
        var configPath = Path.Combine(_tempDir, "filmstruck.json");
        File.WriteAllText(configPath, @"{
            ""username"": ""myname"",
            ""siteTitle"": ""My Film Log""
        }");

        var config = _service.LoadConfig(_tempDir);

        Assert.That(config.Username, Is.EqualTo("myname"));
        Assert.That(config.SiteTitle, Is.EqualTo("My Film Log"));
    }

    [Test]
    public void SaveConfig_RoundTrips()
    {
        var original = new FilmStruckConfig
        {
            Username = "testuser",
            SiteTitle = "Test Site"
        };

        _service.SaveConfig(original, _tempDir);
        var loaded = _service.LoadConfig(_tempDir);

        Assert.That(loaded.Username, Is.EqualTo(original.Username));
        Assert.That(loaded.SiteTitle, Is.EqualTo(original.SiteTitle));
    }
}
