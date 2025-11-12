namespace Graphene_Group_Project.Data.Entities
{
    public class DataFile
    {
        public long FileId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public string? FileHash { get; set; }
        public DateTime? FirstTimestampUtc { get; set; }
        public DateTime? LastTimestampUtc { get; set; }
        public DateTime ImportedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<PressureFrame> Frames { get; set; } = new List<PressureFrame>();
    }
}
