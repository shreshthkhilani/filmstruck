using Spectre.Console;

namespace FilmStruck.Cli.Services;

public class PosterSelectionService
{
    private const string TmdbImageBaseUrl = "https://image.tmdb.org/t/p/w780";
    private const int MaxPosters = 5;

    public string? SelectPoster(string title, string? year, int movieId, List<TmdbPoster> posters, string? currentPoster)
    {
        if (posters.Count <= 1)
        {
            return posters.Count == 1 ? posters[0].FilePath : currentPoster;
        }

        var yearDisplay = !string.IsNullOrEmpty(year) ? $" ({year})" : "";
        var limitedPosters = posters.Take(MaxPosters).ToList();

        // Display poster list with clickable URLs
        AnsiConsole.MarkupLine($"\nSelect poster for [green]\"{Markup.Escape(title)}\"{yearDisplay}[/]:\n");

        for (int i = 0; i < limitedPosters.Count; i++)
        {
            var poster = limitedPosters[i];
            var number = i + 1;
            var defaultLabel = i == 0 ? " [yellow](Default)[/]" : "";
            var resolution = $"{poster.Width}x{poster.Height}";
            var language = FormatLanguage(poster.Language);
            var url = $"{TmdbImageBaseUrl}{poster.FilePath}";

            AnsiConsole.MarkupLine($"  [bold]{number}.[/]{defaultLabel} {resolution} • {language}");
            AnsiConsole.MarkupLine($"     [link={url}]{url}[/]");
            AnsiConsole.WriteLine();
        }

        var selected = AnsiConsole.Prompt(
            new TextPrompt<string>($"Enter poster number (1-{limitedPosters.Count}) or 's' to skip:")
                .DefaultValue("1")
                .Validate(input =>
                {
                    if (input.Equals("s", StringComparison.OrdinalIgnoreCase))
                        return ValidationResult.Success();
                    if (int.TryParse(input, out int num) && num >= 1 && num <= limitedPosters.Count)
                        return ValidationResult.Success();
                    return ValidationResult.Error($"Please enter a number between 1 and {limitedPosters.Count}, or 's' to skip");
                }));

        if (selected.Equals("s", StringComparison.OrdinalIgnoreCase))
        {
            return currentPoster;
        }

        if (int.TryParse(selected, out int index))
        {
            return limitedPosters[index - 1].FilePath;
        }

        return currentPoster;
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

}
