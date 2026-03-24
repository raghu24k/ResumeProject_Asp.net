using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeProject.Models
{
    public class Certification
    {
        public int Id { get; set; }

        [ForeignKey("Resume")]
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }

        [MaxLength(200)]
        public string? CertificationName { get; set; }

        [MaxLength(200)]
        public string? IssuingOrganization { get; set; }

        [MaxLength(20)]
        public string? DateEarned { get; set; }

        [MaxLength(500)]
        public string? CredentialLink { get; set; }
    }
}