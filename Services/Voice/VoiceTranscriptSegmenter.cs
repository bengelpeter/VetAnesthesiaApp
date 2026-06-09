using System.Text.RegularExpressions;

namespace VetAnesthesiaApp.Services.Voice;

public static class VoiceTranscriptSegmenter
{
    private static readonly string[] FieldStarts =
    {
        "heart rate", "hr",
        "respiratory rate", "resp rate", "rr",
        "spo2", "sat", "sats", "oxygen saturation", "oxygen sat",
        "etco2", "co2", "end tidal", "end tidal co2",
        "iso", "isoflurane", "i sell", "gas", "inhalant",
        "o2", "oxygen", "oxygen flow",
        "temperature", "temp",
        "systolic", "sys",
        "diastolic", "dia",
        "map", "mean arterial pressure",
        "note",
        "next bucket", "next time", "new time"
    };

    public static List<string> Split(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return new List<string>();

        var normalized = transcript.Trim();
        var indexes = new List<int> { 0 };

        foreach (var field in FieldStarts)
        {
            var pattern = $@"\b{Regex.Escape(field)}\b";
            foreach (Match match in Regex.Matches(normalized, pattern, RegexOptions.IgnoreCase))
            {
                if (match.Index <= 0)
                    continue;

                indexes.Add(match.Index);
            }
        }

        indexes = indexes.Distinct().OrderBy(x => x).ToList();

        var result = new List<string>();

        for (int i = 0; i < indexes.Count; i++)
        {
            var start = indexes[i];
            var end = i < indexes.Count - 1 ? indexes[i + 1] : normalized.Length;
            var piece = normalized[start..end].Trim();

            if (!string.IsNullOrWhiteSpace(piece))
                result.Add(piece);
        }

        return result;
    }
}
