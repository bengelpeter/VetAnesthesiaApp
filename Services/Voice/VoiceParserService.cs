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
                if (normalized.StartsWith(alias + " "))
                {
                    var valueText = normalized[(alias.Length + 1)..].Trim();
                    var parsedNumber = _numberParser.Parse(valueText);

                    if (parsedNumber is null)
                        return Fail(transcript, normalized, $"Could not parse value for {kvp.Key}.");

                    return new VoiceCommandResult
                    {
                        CommandType = VoiceCommandType.FieldValue,
                        RawTranscript = transcript,
                        NormalizedTranscript = normalized,
                        FieldKey = kvp.Key,
                        ParsedValueText = valueText,
                        ParsedNumericValue = parsedNumber,
                        IsSuccess = true,
                        StatusMessage = "Field value parsed."
                    };
                }
            }
        }

        return Fail(transcript, normalized, "No command match.");
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim().ToLowerInvariant();

        // remove punctuation
        normalized = normalized.Replace("%", " ");
        normalized = normalized.Replace(",", " ");
        normalized = normalized.Replace(":", " ");
        normalized = normalized.Replace(";", " ");
        normalized = normalized.Replace("-", " ");
        normalized = normalized.Replace(".", " ");

        // oxygen variations
        normalized = normalized.Replace("o two", "o2");
        normalized = normalized.Replace("o 2", "o2");

        // spo2 variations
        normalized = normalized.Replace("s p o 2", "spo2");
        normalized = normalized.Replace("s p o two", "spo2");

        // etco2 variations
        normalized = normalized.Replace("e t c o 2", "etco2");
        normalized = normalized.Replace("e t c o two", "etco2");

        // common speech mis-hearings
        normalized = normalized.Replace("i sell", "isoflurane");
        normalized = normalized.Replace("i so", "iso");
        normalized = normalized.Replace("ice so", "iso");

        // saturation shortcuts
        normalized = normalized.Replace("oxygen sat", "oxygen saturation");

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