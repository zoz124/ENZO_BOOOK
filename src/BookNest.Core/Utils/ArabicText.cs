using System.Globalization;
using System.Text;

namespace BookNest.Core.Utils;

/// <summary>
/// Arabic-aware search normalization (T-67).
/// Users type "احمد" and expect "أحمد"; they type "مصطفي" and expect "مصطفى".
/// The normalized form is persisted per book so the comparison happens in SQL.
/// </summary>
public static class ArabicText
{
    private static readonly char[] Diacritics =
    {
        '\u064B', '\u064C', '\u064D', '\u064E', '\u064F', '\u0650', '\u0651',
        '\u0652', '\u0653', '\u0654', '\u0655', '\u0670', '\u0640'
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var raw in value)
        {
            if (Array.IndexOf(Diacritics, raw) >= 0)
            {
                continue;
            }

            builder.Append(raw switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                'ـ' => ' ',
                _ => raw
            });
        }

        // Collapse whitespace and lower-case (Latin titles/authors are searchable too).
        var collapsed = string.Join(' ', builder
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return collapsed.ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>Normalized haystack stored on the book row: title + author + ISBN.</summary>
    public static string BuildSearchText(string? title, string? author, string? isbn)
        => $"{Normalize(title)} {Normalize(author)} {Normalize(isbn)}".Trim();
}
