using System.Text.RegularExpressions;
using VetAnesthesiaApp.Configuration;

namespace VetAnesthesiaApp.Services.Voice;

public class VoiceParserService : IVoiceParserService
{
    private readonly ITextToNumberParser _numberParser;

    public VoiceParserService(ITextToNumberParser numberParser)
    {
        _numberParser = numberParser;
    }

    public VoiceCommandResult Parse(string transcript, DateTime now)
    {
        var normalized = Normalize(transcript);

        if (string.IsNullOrWhiteSpace(normalized))
            return Fail(transcript, normalized, "Empty transcript.");

        if (normalized.StartsWith("note "))
        {
            return new VoiceCommandResult
            {
                CommandType = VoiceCommandType.Note,
                RawTranscript = transcript,
                NormalizedTranscript = normalized,
                NoteText = normalized["note ".Length..].Trim(),
                IsSuccess = true,
                StatusMessage = "Note parsed."
            };
        }

        if (normalized == "next time" || normalized == "next bucket" || normalized == "new time")
        {
            return new VoiceCommandResult
            {
                CommandType = VoiceCommandType.NextBucket,
                RawTranscript = transcript,
                NormalizedTranscript = normalized,
                IsSuccess = true,
                StatusMessage = "Next bucket command parsed."
            };
        }

        if (normalized == "undo")
        {
            return new VoiceCommandResult
            {
                CommandType = VoiceCommandType.Undo,
                RawTranscript = transcript,
                NormalizedTranscript = normalized,
                IsSuccess = true,
                StatusMessage = "Undo command parsed."
            };
        }

        foreach (var kvp in VoiceFieldAliases.FieldAliases)
        {
            foreach (var alias in kvp.Value.OrderByDescending(x => x.Length))
            {
                var match = Regex.Match(normalized, $@"\b{Regex.Escape(alias)}\s+(.+)");

                if (!match.Success)
                    continue;

                var valueText = match.Groups[1].Value.Trim();

                var tokens = valueText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Try a few token lengths before giving up.
                // This prevents "oxygen" from stealing "oxygen saturation 98".
                for (int take = Math.Min(4, tokens.Length); take >= 1; take--)
                {
                    var candidate = string.Join(" ", tokens.Take(take));
                    var parsedNumber = _numberParser.Parse(candidate);

                    if (parsedNumber is not null)
                    {
                        return new VoiceCommandResult
                        {
                            CommandType = VoiceCommandType.FieldValue,
                            RawTranscript = transcript,
                            NormalizedTranscript = normalized,
                            FieldKey = kvp.Key,
                            ParsedValueText = candidate,
                            ParsedNumericValue = parsedNumber,
                            IsSuccess = true,
                            StatusMessage = "Field value parsed."
                        };
                    }
                }

                // IMPORTANT:
                // do not fail yet — keep trying other aliases
            }
        }

        return Fail(transcript, normalized, "No command match.");
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim().ToLowerInvariant();

        // punctuation cleanup
        normalized = normalized.Replace("%", " ");
        normalized = normalized.Replace(",", " ");
        normalized = normalized.Replace(":", " ");
        normalized = normalized.Replace(";", " ");
        normalized = normalized.Replace("-", " ");
        normalized = normalized.Replace(".", " ");
        normalized = normalized.Replace("/", " ");
        normalized = normalized.Replace("\\", " ");

        // common phonetic / speech-engine fixes
        normalized = normalized.Replace("i sell", "isoflurane");
        normalized = normalized.Replace("i so", "iso");
        normalized = normalized.Replace("ice so", "iso");
        normalized = normalized.Replace("eyes so", "iso");

        normalized = normalized.Replace("o two", "o2");
        normalized = normalized.Replace("o 2", "o2");

        normalized = normalized.Replace("co two", "co2");
        normalized = normalized.Replace("co too", "co2");
        normalized = normalized.Replace("c o 2", "co2");

        normalized = normalized.Replace("s p o two", "spo2");
        normalized = normalized.Replace("s p o 2", "spo2");
        normalized = normalized.Replace("s p o too", "spo2");

        normalized = normalized.Replace("e t c o two", "etco2");
        normalized = normalized.Replace("e t c o 2", "etco2");
        normalized = normalized.Replace("e t c o too", "etco2");

        normalized = normalized.Replace("oxygen sat", "oxygen saturation");
        normalized = normalized.Replace("pulse ox", "pulse ox");

        normalized = normalized.Replace("dye ", "dia ");
        normalized = normalized.Replace("diya ", "dia ");
        normalized = normalized.Replace("sis ", "sys ");

        normalized = normalized.Replace("m a p", "map");

        // fix glued field + number cases
        normalized = Regex.Replace(normalized, @"\b(spo2)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(etco2)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(o2)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(co2)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(iso)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(isoflurane)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(hr)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(rr)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(sys)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(dia)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(map)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(temp)(\d+)\b", "$1 $2");

        // fix longer field phrases glued to numbers
        normalized = Regex.Replace(normalized, @"\b(heart rate)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(respiratory rate)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(resp rate)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(oxygen saturation)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(oxygen flow)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(end tidal co2)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(end tidal)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(temperature)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(systolic)(\d+)\b", "$1 $2");
        normalized = Regex.Replace(normalized, @"\b(diastolic)(\d+)\b", "$1 $2");

        // collapse whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    private static VoiceCommandResult Fail(string raw, string normalized, string message)
    {
        return new VoiceCommandResult
        {
            RawTranscript = raw,
            NormalizedTranscript = normalized,
            IsSuccess = false,
            StatusMessage = message
        };
    }
}