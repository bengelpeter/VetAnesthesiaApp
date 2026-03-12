using System;
using System.Collections.Generic;
using System.Linq;

namespace VetAnesthesiaApp.Services.Voice;

public class SpokenNumberParser : ITextToNumberParser
{
    private static readonly Dictionary<string, int> NumberMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zero"] = 0,
        ["oh"] = 0,
        ["one"] = 1,
        ["two"] = 2,
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
        ["six"] = 6,
        ["seven"] = 7,
        ["eight"] = 8,
        ["nine"] = 9,
        ["ten"] = 10,
        ["eleven"] = 11,
        ["twelve"] = 12,
        ["thirteen"] = 13,
        ["fourteen"] = 14,
        ["fifteen"] = 15,
        ["sixteen"] = 16,
        ["seventeen"] = 17,
        ["eighteen"] = 18,
        ["nineteen"] = 19,
        ["twenty"] = 20,
        ["thirty"] = 30,
        ["forty"] = 40,
        ["fifty"] = 50,
        ["sixty"] = 60,
        ["seventy"] = 70,
        ["eighty"] = 80,
        ["ninety"] = 90
    };

    public decimal? Parse(string valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            return null;

        valueText = valueText.Trim().ToLowerInvariant();

        if (decimal.TryParse(valueText, out var direct))
            return direct;

        if (valueText.Contains("point"))
            return ParseDecimalWords(valueText);

        return ParseWholeWords(valueText);
    }

    private decimal? ParseDecimalWords(string text)
    {
        var parts = text.Split("point", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return null;

        var whole = ParseWholeWords(parts[0]);
        if (whole is null)
            return null;

        var decimalTokens = parts[1]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();

        var decimalDigits = "";

        foreach (var token in decimalTokens)
        {
            if (!NumberMap.TryGetValue(token, out var digit) || digit > 9)
                return null;

            decimalDigits += digit.ToString();
        }

        if (string.IsNullOrWhiteSpace(decimalDigits))
            return null;

        if (decimal.TryParse($"{whole}.{decimalDigits}", out var result))
            return result;

        return null;
    }

    private decimal? ParseWholeWords(string text)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Select(t => t.Trim())
                         .ToList();

        if (tokens.Count == 0)
            return null;

        if (tokens.Count == 1)
        {
            if (NumberMap.TryGetValue(tokens[0], out var single))
                return single;
        }

        if (tokens.Count == 2)
        {
            // "ninety eight" = 98
            if (NumberMap.TryGetValue(tokens[0], out var first) &&
                NumberMap.TryGetValue(tokens[1], out var second))
            {
                if (first >= 20 && first % 10 == 0 && second <= 9)
                    return first + second;

                // "one twenty" = 120
                if (first <= 9 && second >= 20 && second % 10 == 0)
                    return first * 100 + second;
            }
        }

        if (tokens.Count == 3)
        {
            // "one twenty five" = 125
            if (NumberMap.TryGetValue(tokens[0], out var first) &&
                NumberMap.TryGetValue(tokens[1], out var second) &&
                NumberMap.TryGetValue(tokens[2], out var third))
            {
                if (first <= 9 && second >= 20 && second % 10 == 0 && third <= 9)
                    return first * 100 + second + third;

                // "one oh five" = 105
                if (first <= 9 && second == 0 && third <= 9)
                    return first * 100 + third;
            }
        }

        return null;
    }
}