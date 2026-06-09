using System;
using System.Collections.Generic;
using System.Text;

namespace VetAnesthesiaApp.Models
{
    public class SessionExportModel
    {
        public Animal Animal { get; set; } = new();
        public AnesthesiaSession Session { get; set; } = new();
        public List<AnesthesiaBucket> Buckets { get; set; } = new();
        public byte[]? GraphImageBytes { get; set; }
    }
}
