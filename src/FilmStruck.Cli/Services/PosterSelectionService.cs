using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace FilmStruck.Cli.Services;

public class PosterSelectionService
{
    private const string DefaultOption = "[[Default]] Use TMDB's primary poster";
    private const string PreviewOption = "[[Preview]] Open posters in browser";
    private const string SkipOption = "[[Skip]] Keep current poster";
    private const string Separator = "────────────────────────────────";

    public string? SelectPoster(string title, string? year, int movieId, List<TmdbPoster> posters, string? currentPoster)
    {
        if (posters.Count <= 1)
        {
            return posters.Count == 1 ? posters[0].FilePath : currentPoster;
        }

        var yearDisplay = !string.IsNullOrEmpty(year) ? $" ({year})" : "";

        while (true)
        {
            var choices = BuildChoices(posters);

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select poster for [green]\"{Markup.Escape(title)}\"{yearDisplay}[/]:")
                    .PageSize(15)
                    .AddChoices(choices));

            if (selected == PreviewOption)
            {
                OpenBrowser($"https://www.themoviedb.org/movie/{movieId}/images/posters");
                AnsiConsole.MarkupLine("[dim]Opened poster gallery in browser. Return here to make your selection.[/]\n");
                continue;
            }

            if (selected == DefaultOption)
            {
                return posters[0].FilePath;
            }

            if (selected == SkipOption)
            {
                return currentPoster;
            }

            // Find the selected poster by matching the formatted string
            var posterIndex = choices.IndexOf(selected) - 3; // Account for Default, Preview, Separator
            if (posterIndex >= 0 && posterIndex < posters.Count)
            {
                return posters[posterIndex].FilePath;
            }

            return currentPoster;
        }
    }

    private List<string> BuildChoices(List<TmdbPoster> posters)
    {
        var choices = new List<string>
        {
            DefaultOption,
            PreviewOption,
            Separator
        };

        foreach (var poster in posters.Take(10)) // Limit to top 10 posters
        {
            choices.Add(FormatPosterOption(poster));
        }

        choices.Add(Separator);
        choices.Add(SkipOption);

        return choices;
    }

    private string FormatPosterOption(TmdbPoster poster)
    {
        var resolution = $"{poster.Width}x{poster.Height}";
        var language = FormatLanguage(poster.Language);
        var rating = $"\u2605 {poster.VoteAverage:F1} ({poster.VoteCount} votes)";

        var parts = new List<string> { resolution, language, rating };

        if (!string.IsNullOrEmpty(poster.Contributor))
        {
            parts.Add($"by {poster.Contributor}");
        }

        return string.Join(" \u2022 ", parts);
    }

    private string FormatLanguage(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return "No text";

        return languageCode.ToLowerInvariant() switch
        {
            "en" => "English",
            "es" => "Spanish",
            "fr" => "French",
            "de" => "German",
            "it" => "Italian",
            "pt" => "Portuguese",
            "ja" => "Japanese",
            "ko" => "Korean",
            "zh" => "Chinese",
            "ru" => "Russian",
            "ar" => "Arabic",
            "hi" => "Hindi",
            "nl" => "Dutch",
            "sv" => "Swedish",
            "pl" => "Polish",
            "tr" => "Turkish",
            "th" => "Thai",
            "vi" => "Vietnamese",
            "cs" => "Czech",
            "el" => "Greek",
            "he" => "Hebrew",
            "hu" => "Hungarian",
            "id" => "Indonesian",
            "da" => "Danish",
            "fi" => "Finnish",
            "no" => "Norwegian",
            "uk" => "Ukrainian",
            "ro" => "Romanian",
            "bg" => "Bulgarian",
            "hr" => "Croatian",
            "sk" => "Slovak",
            "sl" => "Slovenian",
            "sr" => "Serbian",
            "ms" => "Malay",
            "tl" => "Tagalog",
            "fa" => "Persian",
            "bn" => "Bengali",
            "ta" => "Tamil",
            "te" => "Telugu",
            _ => languageCode.ToUpperInvariant()
        };
    }

    private void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
        }
        catch
        {
            AnsiConsole.MarkupLine($"[yellow]Could not open browser. Visit:[/] {url}");
        }
    }
}
