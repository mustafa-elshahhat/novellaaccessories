using System.Linq;

namespace Novella.Domain.Services;

/// <summary>
/// Normalizes phone numbers to a canonical digits-only form (Egypt-aware) used for
/// uniqueness and lookups. e.g. "010 1234 5678" / "+201012345678" -> "201012345678".
/// </summary>
public static class PhoneNumberNormalizer
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00")) digits = digits[2..];

        // Egyptian local form (0XXXXXXXXXX) -> country code 20.
        if (digits.StartsWith("0")) digits = "20" + digits[1..];

        return digits;
    }
}
