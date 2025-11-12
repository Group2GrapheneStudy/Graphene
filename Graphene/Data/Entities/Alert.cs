namespace Graphene_Group_Project.Data.Entities
{
    public class Alert
    {
        public long AlertId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;
        public long? FrameId { get; set; }
        public PressureFrame? Frame { get; set; }

        public DateTime TriggeredUtc { get; set; }
        public byte Severity { get; set; }  // 1..3
        public int? MaxPressure { get; set; }
        public int? PixelsAboveThr { get; set; }
        public string? RegionJson { get; set; }

        public byte Status { get; set; } = 0;   // 0 new, 1 ack, 2 resolved
        public string? Notes { get; set; }
    }
}
