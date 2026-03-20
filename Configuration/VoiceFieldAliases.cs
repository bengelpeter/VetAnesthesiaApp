using VetAnesthesiaApp.Constants;

namespace VetAnesthesiaApp.Configuration
{
    public static class VoiceFieldAliases
    {
        public static readonly Dictionary<string, string[]> FieldAliases = new()
        {
            [AnesthesiaFieldKeys.IsoPercent] = new[]
            {
                "isoflurane", "iso", "i sell", "i so", "ice so", "eyes so", "gas", "inhalant"
            },

            [AnesthesiaFieldKeys.OxygenFlowRate] = new[]
            {
                "oxygen flow", "o2", "o two", "oxygen", "flow"
            },

            [AnesthesiaFieldKeys.Etco2] = new[]
            {
                "end tidal co2",
                "end tidal",
                "and tidal",
                "etco2",
                "e t c o 2",
                "co2",
                "co two"
            },

            [AnesthesiaFieldKeys.Spo2] = new[]
            {
                "oxygen saturation", "oxygen sat", "pulse ox", "spo2", "s p o 2", "s p o two", "sat", "sats"
            },

            [AnesthesiaFieldKeys.Temperature] = new[]
            {
                "temperature", "temp", "temps"
            },

            [AnesthesiaFieldKeys.HeartRate] = new[]
            {
                "heart rate", "hr", "rate"
            },

            [AnesthesiaFieldKeys.RespiratoryRate] = new[]
            {
                "respiratory rate", "resp rate", "respirations", "rr", "resp", "rest", "breaths", "breathing rate"
            },

            [AnesthesiaFieldKeys.SystolicBp] = new[]
            {
                "systolic", "sys", "sis", "top number"
            },

            [AnesthesiaFieldKeys.DiastolicBp] = new[]
            {
                "diastolic", "dia", "dye", "diya", "bottom number", "die"
            },

            [AnesthesiaFieldKeys.Map] = new[]
            {
                "mean arterial pressure", "map", "m a p"
            },

            [AnesthesiaFieldKeys.Notes] = new[]
            {
                "note", "notes"
            }
        };
    }
}