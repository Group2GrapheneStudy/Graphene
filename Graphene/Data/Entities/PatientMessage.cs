using System;

namespace Graphene_Group_Project.Data.Entities
{
    /// <summary>
    /// Represents a message between a patient and a clinician.
    /// Stored in the database so the conversation persists.
    /// </summary>
    public class PatientMessage
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the patient this message belongs to.
        /// </summary>
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = default!;

        /// <summary>
        /// Optional link to a specific pressure frame if you ever want
        /// to anchor comments to an exact timestamp.
        /// </summary>
        public long? FrameId { get; set; }
        public PressureFrame? Frame { get; set; }

        /// <summary>
        /// When the message was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// "Patient" or "Clinician" – who wrote the message.
        /// </summary>
        public string FromRole { get; set; } = string.Empty;

        /// <summary>
        /// Human-friendly name of the sender (patient full name or clinician name).
        /// </summary>
        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// Message content.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
