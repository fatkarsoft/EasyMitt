namespace EasyMitt.Domain.Payments;

public static class IbanPolicy
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var buffer = new char[value.Length];
        var index = 0;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || character is '-' or '.' or '_')
            {
                continue;
            }

            buffer[index++] = char.ToUpperInvariant(character);
        }

        return new string(buffer, 0, index);
    }

    public static bool IsValid(string? value)
    {
        var iban = Normalize(value);
        if (iban.Length is < 15 or > 34)
        {
            return false;
        }

        if (!char.IsAsciiLetter(iban[0]) ||
            !char.IsAsciiLetter(iban[1]) ||
            !char.IsAsciiDigit(iban[2]) ||
            !char.IsAsciiDigit(iban[3]))
        {
            return false;
        }

        if (iban.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            return false;
        }

        return HasValidChecksum(iban);
    }

    private static bool HasValidChecksum(string iban)
    {
        var remainder = 0;
        foreach (var character in iban[4..].Concat(iban[..4]))
        {
            if (char.IsAsciiDigit(character))
            {
                remainder = ApplyDigit(remainder, character - '0');
                continue;
            }

            var value = character - 'A' + 10;
            remainder = ApplyDigit(remainder, value / 10);
            remainder = ApplyDigit(remainder, value % 10);
        }

        return remainder == 1;
    }

    private static int ApplyDigit(int remainder, int digit) => ((remainder * 10) + digit) % 97;
}
