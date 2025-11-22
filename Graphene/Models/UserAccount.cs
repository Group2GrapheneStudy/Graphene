using System.ComponentModel.DataAnnotations;

namespace Graphene_Group_Project.Models
{
    public class UserAccount
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        // For coursework this can be plain text; in real life use hashing
        [Required, StringLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Role { get; set; } = string.Empty; // "Patient", "Clinician", "Admin"
    }
}
