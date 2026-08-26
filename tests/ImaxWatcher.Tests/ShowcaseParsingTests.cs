using ImaxWatcher.Services.Parsing;

namespace ImaxWatcher.Tests;

// The Spanish string literals below are verbatim site content, not prose:
// they are exactly what Showcase renders, captured from the live page.

public class TryParseDateTests
{
    [Theory]
    // Real format Showcase serves today.
    [InlineData("martes, 25 de agosto de 2026", 2026, 8, 25)]
    [InlineData("miércoles, 2 de septiembre de 2026", 2026, 9, 2)]
    [InlineData("domingo, 30 de agosto de 2026", 2026, 8, 30)]
    // Unaccented spelling and "setiembre", both common in Argentina.
    [InlineData("miercoles, 2 de setiembre de 2026", 2026, 9, 2)]
    // Numeric fallback formats.
    [InlineData("2/9/2026", 2026, 9, 2)]
    [InlineData("02/09/2026", 2026, 9, 2)]
    public void RecognizesKnownFormats(string text, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), ShowcaseParsing.TryParseDate(text));
    }

    [Theory]
    [InlineData("Seleccione Día...")]
    [InlineData("")]
    [InlineData("   ")]
    // Abbreviated months are NOT supported today. If Showcase moves to this format the
    // watcher stops seeing showings, and this test documents that boundary.
    [InlineData("mié 02 sep 2026")]
    public void ReturnsNullWhenFormatIsUnrecognized(string text)
    {
        Assert.Null(ShowcaseParsing.TryParseDate(text));
    }

    [Theory]
    [InlineData("31 de febrero de 2026")]
    [InlineData("32 de agosto de 2026")]
    public void RejectsImpossibleDatesInsteadOfThrowing(string text)
    {
        Assert.Null(ShowcaseParsing.TryParseDate(text));
    }
}

public class ParseTimesTests
{
    [Fact]
    public void ExtractsSortsAndDeduplicates()
    {
        var times = ShowcaseParsing.ParseTimes(["22:35", "13:05", "19:00", "13:05"]);

        Assert.Equal(
            [new TimeOnly(13, 5), new TimeOnly(19, 0), new TimeOnly(22, 35)],
            times);
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("25:99")]
    [InlineData("sin horario")]
    [InlineData("")]
    public void IgnoresTextWithoutAValidTime(string text)
    {
        Assert.Empty(ShowcaseParsing.ParseTimes([text]));
    }

    [Fact]
    public void DoesNotMatchATimeGluedToOtherDigits()
    {
        Assert.Empty(ShowcaseParsing.ParseTimes(["112:305"]));
    }
}

public class IsRealOptionTests
{
    [Theory]
    [InlineData("Seleccione Día...")]
    [InlineData("Elegir formato")]
    [InlineData("-")]
    [InlineData("   ")]
    public void DiscardsPlaceholders(string text)
    {
        Assert.False(ShowcaseParsing.IsRealOption(new DomOption { Text = text }));
    }

    [Fact]
    public void DiscardsDisabledOptions()
    {
        Assert.False(ShowcaseParsing.IsRealOption(
            new DomOption { Text = "IMAX-Subtitulado", Disabled = true }));
    }

    [Fact]
    public void AcceptsARealOption()
    {
        Assert.True(ShowcaseParsing.IsRealOption(
            new DomOption { Text = "IMAX-Subtitulado", Value = "8" }));
    }
}

public class FindOptionTests
{
    private static readonly DomOption[] Cinemas =
    [
        new() { Value = "1", Text = "Showcase Belgrano" },
        new() { Value = "18", Text = "IMAX Theatre (Norcenter)" },
        new() { Value = "20", Text = "Showcase Norcenter" }
    ];

    [Fact]
    public void PrefersTheExactMatchOverThePartialOne()
    {
        var match = ShowcaseParsing.FindOption(Cinemas, "IMAX Theatre (Norcenter)");

        Assert.Equal("18", match?.Value);
    }

    [Fact]
    public void IgnoresAccentsAndCasing()
    {
        var options = new[] { new DomOption { Value = "5875", Text = "La Odisea" } };

        Assert.Equal("5875", ShowcaseParsing.FindOption(options, "LA ODÍSEA")?.Value);
    }

    [Fact]
    public void FallsBackToPartialMatchWhenThereIsNoExactOne()
    {
        Assert.Equal("18", ShowcaseParsing.FindOption(Cinemas, "Theatre (Norcenter)")?.Value);
    }

    [Fact]
    public void PartialMatchTakesTheFirstInDomOrder()
    {
        // "Norcenter" appears in two cinemas. The first one in the select wins, not the
        // "best" one. That is why the config uses the full name: an ambiguous partial
        // picks on its own.
        Assert.Equal("18", ShowcaseParsing.FindOption(Cinemas, "Norcenter")?.Value);
    }

    [Fact]
    public void ReturnsNullWhenNothingMatches()
    {
        Assert.Null(ShowcaseParsing.FindOption(Cinemas, "Cinemark Palermo"));
    }
}

public class NormalizeTests
{
    [Theory]
    [InlineData("MIÉRCOLES", "miercoles")]
    [InlineData("  La Odisea  ", "la odisea")]
    [InlineData("Añejo", "anejo")]
    public void LowercasesStripsAccentsAndTrims(string input, string expected)
    {
        Assert.Equal(expected, ShowcaseParsing.Normalize(input));
    }
}
