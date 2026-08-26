using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ImaxWatcher.Services.Parsing;

/// <summary>
/// Pure parsing logic for Showcase's HTML. Kept out of <see cref="ShowcaseReader"/> so
/// it can be tested without launching a browser: the date and time formats that break
/// when the site changes all live here.
/// </summary>
internal static partial class ShowcaseParsing
{
    private static readonly Dictionary<string, int> SpanishMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enero"] = 1,
        ["febrero"] = 2,
        ["marzo"] = 3,
        ["abril"] = 4,
        ["mayo"] = 5,
        ["junio"] = 6,
        ["julio"] = 7,
        ["agosto"] = 8,
        ["septiembre"] = 9,
        ["setiembre"] = 9,
        ["octubre"] = 10,
        ["noviembre"] = 11,
        ["diciembre"] = 12
    };

    /// <summary>Lowercased and accent-stripped, for comparing text from the site.</summary>
    internal static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Parses "martes, 25 de agosto de 2026" as well as dd/MM/yyyy.
    /// Returns <c>null</c> when the format is not recognized.
    /// </summary>
    internal static DateOnly? TryParseDate(string text)
    {
        var match = SpanishLongDateRegex().Match(text);

        if (match.Success)
        {
            var day = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var monthText = Normalize(match.Groups[2].Value);
            var year = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            if (SpanishMonths.TryGetValue(monthText, out var month)
                && day >= 1
                && day <= DateTime.DaysInMonth(year, month))
            {
                return new DateOnly(year, month, day);
            }
        }

        string[] formats = ["d/M/yyyy", "dd/MM/yyyy", "d/M/yy", "dd/MM/yy"];

        return DateOnly.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    /// <summary>Drops placeholders such as "Seleccione Día..." and disabled options.</summary>
    internal static bool IsRealOption(DomOption option)
    {
        if (option.Disabled || string.IsNullOrWhiteSpace(option.Text))
            return false;

        var text = Normalize(option.Text);

        return !text.Contains("seleccione", StringComparison.Ordinal)
               && !text.Contains("elegir", StringComparison.Ordinal)
               && text is not "-";
    }

    /// <summary>Exact match on normalized text; failing that, the first one containing it.</summary>
    internal static DomOption? FindOption(IEnumerable<DomOption> options, string requestedText)
    {
        var requested = Normalize(requestedText);
        var materialized = options as IReadOnlyList<DomOption> ?? [.. options];

        return materialized.FirstOrDefault(x => Normalize(x.Text) == requested)
               ?? materialized.FirstOrDefault(x => Normalize(x.Text).Contains(requested, StringComparison.Ordinal));
    }

    /// <summary>Extracts HH:mm values from a set of texts, sorted and deduplicated.</summary>
    internal static IReadOnlyList<TimeOnly> ParseTimes(IEnumerable<string> texts)
    {
        var found = new SortedSet<TimeOnly>();

        foreach (var text in texts)
        {
            foreach (Match match in TimeRegex().Matches(text))
            {
                if (TimeOnly.TryParseExact(
                        match.Value,
                        ["H:mm", "HH:mm"],
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var time))
                {
                    found.Add(time);
                }
            }
        }

        return [.. found];
    }

    [GeneratedRegex(@"(\d{1,2})\s+de\s+([a-zA-ZáéíóúüñÁÉÍÓÚÜÑ]+)\s+de\s+(\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex SpanishLongDateRegex();

    [GeneratedRegex(@"(?<!\d)(?:[01]?\d|2[0-3]):[0-5]\d(?!\d)")]
    private static partial Regex TimeRegex();
}
