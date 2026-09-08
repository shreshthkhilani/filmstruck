using FilmStruck.Cli;
using FilmStruck.Cli.Commands;
using NUnit.Framework;

namespace FilmStruck.Cli.Tests.Commands;

[TestFixture]
public class BuildCommandTests
{
    [Test]
    public void JoinLogWithFilms_IncludesEntryWithoutTmdbId()
    {
        var log = new List<Film>
        {
            new Film("9/6/2026", "Festival Short", "Arverne Cinema", "Jack,Eli", null)
        };

        var watchedFilms = BuildCommand.JoinLogWithFilms(log, new Dictionary<int, ApprovedFilm>());

        Assert.That(watchedFilms, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(watchedFilms[0].Title, Is.EqualTo("Festival Short"));
            Assert.That(watchedFilms[0].TmdbId, Is.Empty);
            Assert.That(watchedFilms[0].PosterPath, Is.Empty);
            Assert.That(watchedFilms[0].ReleaseYear, Is.Empty);
            Assert.That(watchedFilms[0].Director, Is.Empty);
        });
    }

    [Test]
    public void JoinLogWithFilms_PreservesLinkedMetadata()
    {
        var log = new List<Film>
        {
            new Film("9/6/2026", "Search Title", "Home", "", 123)
        };
        var films = new Dictionary<int, ApprovedFilm>
        {
            [123] = new ApprovedFilm(123, "Canonical Title", "Director", "2026", "en", "/poster.jpg")
        };

        var watchedFilms = BuildCommand.JoinLogWithFilms(log, films);

        Assert.That(watchedFilms, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(watchedFilms[0].Title, Is.EqualTo("Canonical Title"));
            Assert.That(watchedFilms[0].TmdbId, Is.EqualTo("123"));
            Assert.That(watchedFilms[0].PosterPath, Is.EqualTo("/poster.jpg"));
        });
    }
}
