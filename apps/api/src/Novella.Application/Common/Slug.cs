using System.Globalization;
using System.Text;

namespace Novella.Application.Common;

/// <summary>Unicode-aware slugifier that preserves Arabic letters and lowercases Latin.</summary>
public static class Slug
{
    public static string From(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n")[..8];

        var sb = new StringBuilder(input.Length);
        var lastDash = false;
        foreach (var ch in input.Trim().ToLower(CultureInfo.InvariantCulture))
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash && sb.Length > 0)
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? Guid.NewGuid().ToString("n")[..8] : slug;
    }

    public static string Ensure(string? provided, string fallbackSource)
        => string.IsNullOrWhiteSpace(provided) ? From(fallbackSource) : From(provided);
}
