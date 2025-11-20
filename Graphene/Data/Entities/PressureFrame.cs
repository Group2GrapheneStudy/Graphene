namespace Graphene_Group_Project.Data.Entities
{
    public class PressureFrame
    {
        public long FrameId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;
        public long FileId { get; set; }
        public DataFile File { get; set; } = default!;
        public int FrameIndex { get; set; }
        public DateTime? CapturedUtc { get; set; }
        public byte Width { get; set; } = 32;
        public byte Height { get; set; } = 32;

        // Stored metrics
        public int? PeakPressure { get; set; }
        public int? PixelsAboveThr { get; set; }
        public decimal? ContactAreaPct { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
