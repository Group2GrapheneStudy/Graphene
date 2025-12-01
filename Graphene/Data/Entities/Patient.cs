namespace Graphene_Group_Project.Data.Entities
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string? ExternalUserId { get; set; }
        public string FullName { get; set; } = default!;
        public DateTime? DateOfBirth { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<DataFile> Files { get; set; } = new List<DataFile>();
        public ICollection<PressureFrame> Frames { get; set; } = new List<PressureFrame>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

        /// <summary>
        /// Messages linked to this patient (patient–clinician conversation).
        /// </summary>
        public ICollection<PatientMessage> Messages { get; set; } = new List<PatientMessage>();
    }
}
