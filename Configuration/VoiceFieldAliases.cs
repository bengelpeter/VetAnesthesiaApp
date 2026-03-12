using VetAnesthesiaApp.Constants;

namespace VetAnesthesiaApp.Configuration
{
    public static class VoiceFieldAliases
    {
        public static readonly Dictionary<string, string[]> FieldAliases = new()
        {
            [AnesthesiaFieldKeys.IsoPercent] = new[] { "isoflurane", "iso", "i sell", "gas", "inhalant" },
            [AnesthesiaFieldKeys.OxygenFlowRate] = new[] { "oxygen flow", "o2", "oxygen", "o two" },
            [AnesthesiaFieldKeys.Etco2] = new[] { "end tidal co2", "end tidal", "etco2", "e t c o 2", "co2" },
            [AnesthesiaFieldKeys.Spo2] = new[] { "oxygen saturation", "oxygen sat", "pulse ox", "spo2", "s p o 2" },
            [AnesthesiaFieldKeys.Temperature] = new[] { "temperature", "temp" },
            [AnesthesiaFieldKeys.HeartRate] = new[] { "heart rate", "hr" },
            [AnesthesiaFieldKeys.RespiratoryRate] = new[] { "respiratory rate", "resp rate", "respirations", "rr", "resp" },
            [AnesthesiaFieldKeys.SystolicBp] = new[] { "systolic", "sys" },
            [AnesthesiaFieldKeys.DiastolicBp] = new[] { "diastolic", "dia" },
            [AnesthesiaFieldKeys.Map] = new[] { "mean arterial pressure", "map" },
            [AnesthesiaFieldKeys.Notes] = new[] { "note", "notes" }
        };
    }
}